// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryManager.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Microsoft.Web.LibraryManager.Providers.Cdnjs
{
    /// <summary>Internal use only</summary>
    internal class CdnjsProvider : IProvider
    {
        private const string _downloadUrlFormat = "https://cdnjs.cloudflare.com/ajax/libs/{0}/{1}/{2}"; // https://aka.ms/ezcd7o/{0}/{1}/{2}
        private CdnjsCatalog _catalog;
        private const int _expiresAfterDays = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="CdnjsProvider"/> class.
        /// </summary>
        /// <param name="hostInteraction">The host interaction.</param>
        public CdnjsProvider(IHostInteraction hostInteraction)
        {
            HostInteraction = hostInteraction;
        }

        /// <summary>
        /// The unique identifier of the provider.
        /// </summary>
        public string Id { get; } = "cdnjs";

        /// <summary>
        /// The NuGet Package id for the package including the provider for use by MSBuild.
        /// </summary>
        /// <remarks>
        /// If the provider doesn't have a NuGet package, then return <code>null</code>.
        /// </remarks>
        public string NuGetPackageId { get; } = "Microsoft.Web.LibraryManager.Build";

        /// <summary>
        /// An object specified by the host to interact with the file system etc.
        /// </summary>
        public IHostInteraction HostInteraction { get; }

        internal string CacheFolder
        {
            get { return Path.Combine(HostInteraction.CacheDirectory, Id); }
        }

        /// <summary>
        /// Gets the <see cref="T:Microsoft.Web.LibraryManager.Contracts.ILibraryCatalog" /> for the <see cref="T:Microsoft.Web.LibraryManager.Contracts.IProvider" />. May be <code>null</code> if no catalog is supported.
        /// </summary>
        /// <returns></returns>
        public ILibraryCatalog GetCatalog()
        {
            return _catalog ?? (_catalog = new CdnjsCatalog(this));
        }

        /// <summary>
        /// Installs a library as specified in the <paramref name="desiredState" /> parameter.
        /// </summary>
        /// <param name="desiredState">The details about the library to install.</param>
        /// <param name="cancellationToken">A token that allows for the operation to be cancelled.</param>
        /// <returns>
        /// The <see cref="T:Microsoft.Web.LibraryManager.Contracts.ILibraryInstallationResult" /> from the installation process.
        /// </returns>
        /// <exception cref="InvalidLibraryException"></exception>
        public async Task<ILibraryInstallationResult> InstallAsync(ILibraryInstallationState desiredState, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return LibraryInstallationResult.FromCancelled(desiredState);
            }

            if (!desiredState.IsValid(out IEnumerable<IError> errors))
            {
                return new LibraryInstallationResult(desiredState, errors.ToArray());
            }

            ILibraryInstallationResult result = await UpdateStateAsync(desiredState, cancellationToken);

            if (!result.Success)
            {
                return result;
            }

            desiredState = result.InstallationState;


            result = await HydrateCacheAsync(desiredState, cancellationToken);

            if (!result.Success)
            {
                return result;
            }

            try
            {
                foreach (string file in desiredState.Files)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return LibraryInstallationResult.FromCancelled(desiredState);
                    }

                    if (string.IsNullOrEmpty(file))
                    {
                        return new LibraryInstallationResult(desiredState, PredefinedErrors.CouldNotWriteFile(file));
                    }

                    string path = Path.Combine(desiredState.DestinationPath, file);
                    var sourceStream = new Func<Stream>(() => GetStreamAsync(desiredState, file, cancellationToken).Result);
                    bool writeOk = await HostInteraction.WriteFileAsync(path, sourceStream, desiredState, cancellationToken).ConfigureAwait(false);

                    if (!writeOk)
                    {
                        return new LibraryInstallationResult(desiredState, PredefinedErrors.CouldNotWriteFile(file));
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return new LibraryInstallationResult(desiredState, PredefinedErrors.PathOutsideWorkingDirectory());
            }
            catch (Exception ex)
            {
                HostInteraction.Logger.Log(ex.ToString(), LogLevel.Error);
                return new LibraryInstallationResult(desiredState, PredefinedErrors.UnknownException());
            }

            return LibraryInstallationResult.FromSuccess(desiredState);
        }

        /// <summary>
        /// Updates file set on the passed in ILibraryInstallationState in case user selected to have all files included
        /// </summary>
        /// <param name="desiredState"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ILibraryInstallationResult> UpdateStateAsync(ILibraryInstallationState desiredState, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return LibraryInstallationResult.FromCancelled(desiredState);
            }

            try
            {

                ILibraryCatalog catalog = GetCatalog();
                ILibrary library = await catalog.GetLibraryAsync(desiredState.LibraryId, cancellationToken).ConfigureAwait(false);

                if (library == null)
                {
                    throw new InvalidLibraryException(desiredState.LibraryId, Id);
                }

                if (desiredState.Files != null && desiredState.Files.Count > 0)
                {
                    return LibraryInstallationResult.FromSuccess(desiredState);
                }

                desiredState = new LibraryInstallationState
                {
                    ProviderId = Id,
                    LibraryId = desiredState.LibraryId,
                    DestinationPath = desiredState.DestinationPath,
                    Files = library.Files.Keys.ToList(),
                };
            }
            catch (Exception ex) when (ex is InvalidLibraryException || ex.InnerException is InvalidLibraryException)
            {
                return new LibraryInstallationResult(desiredState, PredefinedErrors.UnableToResolveSource(desiredState.LibraryId, desiredState.ProviderId));
            }
            catch (UnauthorizedAccessException)
            {
                return new LibraryInstallationResult(desiredState, PredefinedErrors.PathOutsideWorkingDirectory());
            }
            catch (Exception ex)
            {
                HostInteraction.Logger.Log(ex.ToString(), LogLevel.Error);
                return new LibraryInstallationResult(desiredState, PredefinedErrors.UnknownException());
            }

            return LibraryInstallationResult.FromSuccess(desiredState);
        }

        private async Task<Stream> GetStreamAsync(ILibraryInstallationState state, string sourceFile, CancellationToken cancellationToken)
        {
            string[] args = state.LibraryId.Split('@');
            string name = args[0];
            string version = args[1];
            string absolute = Path.Combine(CacheFolder, name, version, sourceFile);

            if (File.Exists(absolute))
            {
                return await FileHelpers.OpenFileAsync(absolute, cancellationToken).ConfigureAwait(false);
            }

            return null;
        }

        /// <summary>
        /// Copies ILibraryInstallationState files to cache
        /// </summary>
        /// <param name="state"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<LibraryInstallationResult> HydrateCacheAsync(ILibraryInstallationState state, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return LibraryInstallationResult.FromCancelled(state);
            }

            var tasks = new List<Task>();
            string[] args = state.LibraryId.Split('@');
            string name = args[0];
            string version = args[1];

            string libraryDir = Path.Combine(CacheFolder, name);

            try
            {
                foreach (string sourceFile in state.Files)
                {
                    string cacheFile = Path.Combine(libraryDir, version, sourceFile);
                    string url = string.Format(_downloadUrlFormat, name, version, sourceFile);

                    if (!File.Exists(cacheFile) || File.GetLastWriteTime(cacheFile) < DateTime.Now.AddDays(-_expiresAfterDays))
                    {
                        await FileHelpers.DownloadFileAsync(url, cacheFile, cancellationToken);
                        HostInteraction.Logger.Log(string.Format(Resources.Text.FileWrittenToCache, sourceFile), LogLevel.Operation);
                    }
                }
            }
            catch (ResourceDownloadException ex)
            {
                HostInteraction.Logger.Log(ex.ToString(), LogLevel.Error);
                return new LibraryInstallationResult(state, PredefinedErrors.FailedToDownloadResource(ex.Url));
            }
            catch (Exception ex)
            {
                HostInteraction.Logger.Log(ex.InnerException.ToString(), LogLevel.Error);
                return new LibraryInstallationResult(state, PredefinedErrors.UnknownException());
            }

            return LibraryInstallationResult.FromSuccess(state);
        }
    }
}
