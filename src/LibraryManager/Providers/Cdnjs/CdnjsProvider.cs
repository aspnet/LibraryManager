// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Resources;

namespace Microsoft.Web.LibraryManager.Providers.Cdnjs
{
    /// <summary>Internal use only</summary>
    internal class CdnjsProvider : IProvider
    {
        // TO DO: This should become Provider properties to be passed to CacheService
        private const string _downloadUrlFormat = "https://cdnjs.cloudflare.com/ajax/libs/{0}/{1}/{2}"; // https://aka.ms/ezcd7o/{0}/{1}/{2}

        private CdnjsCatalog _catalog;
        private CacheService _cacheService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CdnjsProvider"/> class.
        /// </summary>
        /// <param name="hostInteraction">The host interaction.</param>
        public CdnjsProvider(IHostInteraction hostInteraction)
        {
            HostInteraction = hostInteraction;
            _cacheService = new CacheService(WebRequestHandler.Instance);
        }

        /// <summary>
        /// The unique identifier of the provider.
        /// </summary>
        public string Id { get; } = "cdnjs";

        /// <summary>
        /// Hint text for the library id.
        /// </summary>
        public string LibraryIdHintText { get; } = Text.CdnjsLibraryIdHintText;

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
        /// Supports libraries with versions.
        /// </summary>
        public bool SupportsLibraryVersions => true;

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
        /// The <see cref="T:Microsoft.Web.LibraryManager.Contracts.ILibraryOperationResult" /> from the installation process.
        /// </returns>
        /// <exception cref="InvalidLibraryException"></exception>
        public async Task<ILibraryOperationResult> InstallAsync(ILibraryInstallationState desiredState, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return LibraryOperationResult.FromCancelled(desiredState);
            }

            //Expand the files property if needed
            ILibraryOperationResult updateResult = await UpdateStateAsync(desiredState, cancellationToken);
            if (!updateResult.Success)
            {
                return updateResult;
            }

            desiredState = updateResult.InstallationState;

            // Refresh cache if needed
            ILibraryOperationResult cacheUpdateResult = await RefreshCacheAsync(desiredState, cancellationToken);
            if (!cacheUpdateResult.Success)
            {
                return cacheUpdateResult;
            }

            // Check if Library is already up tp date
            if (IsLibraryUpToDate(desiredState))
            {
                return LibraryOperationResult.FromUpToDate(desiredState);
            }

            // Write files to destination
            return await WriteToFilesAsync(desiredState, cancellationToken);

        }

        /// <summary>
        /// Returns the CdnjsLibrary's Name
        /// </summary>
        /// <param name="library"></param>
        /// <returns></returns>
        public string GetSuggestedDestination(ILibrary library)
        {
            if (library != null && library is CdnjsLibrary cdnjsLibrary)
            {
                return cdnjsLibrary.Name;
            }

            return string.Empty;
        }

        private async Task<ILibraryOperationResult> WriteToFilesAsync(ILibraryInstallationState state, CancellationToken cancellationToken)
        {
            if (state.Files != null)
            {
                try
                {
                    foreach (string file in state.Files)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return LibraryOperationResult.FromCancelled(state);
                        }

                        if (string.IsNullOrEmpty(file))
                        {
                            return new LibraryOperationResult(state, PredefinedErrors.CouldNotWriteFile(file));
                        }

                        string destinationPath = Path.Combine(state.DestinationPath, file);
                        var sourceStream = new Func<Stream>(() => GetStreamAsync(state, file, cancellationToken).Result);
                        bool writeOk = await HostInteraction.WriteFileAsync(destinationPath, sourceStream, state, cancellationToken).ConfigureAwait(false);

                        if (!writeOk)
                        {
                            return new LibraryOperationResult(state, PredefinedErrors.CouldNotWriteFile(file));
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    return new LibraryOperationResult(state, PredefinedErrors.PathOutsideWorkingDirectory());
                }
                catch (Exception ex)
                {
                    HostInteraction.Logger.Log(ex.ToString(), LogLevel.Error);
                    return new LibraryOperationResult(state, PredefinedErrors.UnknownException());
                }
            }

            return LibraryOperationResult.FromSuccess(state);
        }

        /// <summary>
        /// Updates file set on the passed in ILibraryInstallationState in case user selected to have all files included
        /// </summary>
        /// <param name="desiredState"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ILibraryOperationResult> UpdateStateAsync(ILibraryInstallationState desiredState, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return LibraryOperationResult.FromCancelled(desiredState);
            }

            try
            {
                ILibraryCatalog catalog = GetCatalog();
                ILibrary library = await catalog.GetLibraryAsync(desiredState.LibraryId, cancellationToken).ConfigureAwait(false);

                if (library == null)
                {
                    return new LibraryOperationResult(desiredState, PredefinedErrors.UnableToResolveSource(desiredState.LibraryId, desiredState.ProviderId));
                }

                if (desiredState.Files != null && desiredState.Files.Count > 0)
                {
                    IReadOnlyList<string> invalidFiles = library.GetInvalidFiles(desiredState.Files);
                    if (invalidFiles.Any())
                    {
                        IError invalidFilesError = PredefinedErrors.InvalidFilesInLibrary(desiredState.LibraryId, invalidFiles, library.Files.Keys);
                        return new LibraryOperationResult(desiredState, invalidFilesError);
                    }
                    else
                    {
                        return LibraryOperationResult.FromSuccess(desiredState);
                    }
                }

                desiredState = new LibraryInstallationState
                {
                    ProviderId = Id,
                    LibraryId = desiredState.LibraryId,
                    DestinationPath = desiredState.DestinationPath,
                    Files = library.Files.Keys.ToList(),
                };
            }
            catch (InvalidLibraryException)
            {
                return new LibraryOperationResult(desiredState, PredefinedErrors.UnableToResolveSource(desiredState.LibraryId, desiredState.ProviderId));
            }
            catch (UnauthorizedAccessException)
            {
                return new LibraryOperationResult(desiredState, PredefinedErrors.PathOutsideWorkingDirectory());
            }
            catch (Exception ex)
            {
                HostInteraction.Logger.Log(ex.ToString(), LogLevel.Error);
                return new LibraryOperationResult(desiredState, PredefinedErrors.UnknownException());
            }

            return LibraryOperationResult.FromSuccess(desiredState);
        }

        private async Task<Stream> GetStreamAsync(ILibraryInstallationState state, string sourceFile, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(state.Name) && !string.IsNullOrEmpty(state.Version))
            {
                string absolute = Path.Combine(CacheFolder, state.Name, state.Version, sourceFile);

                if (File.Exists(absolute))
                {
                    return await HostInteraction.ReadFileAsync(absolute, cancellationToken).ConfigureAwait(false);
                }
            }

            return null;
        }

        /// <summary>
        /// Copies ILibraryInstallationState files to cache
        /// </summary>
        /// <param name="state"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<ILibraryOperationResult> RefreshCacheAsync(ILibraryInstallationState state, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return LibraryOperationResult.FromCancelled(state);
            }

            var tasks = new List<Task>();

            try
            {
                if (!string.IsNullOrEmpty(state.Name) && !string.IsNullOrEmpty(state.Version))
                {
                    string libraryDir = Path.Combine(CacheFolder, state.Name);
                    List<CacheServiceMetadata> librariesMetadata = new List<CacheServiceMetadata>();

                    foreach (string sourceFile in state.Files)
                    {
                        string cacheFile = Path.Combine(libraryDir, state.Version, sourceFile);
                        string url = string.Format(_downloadUrlFormat, state.Name, state.Version, sourceFile);

                        CacheServiceMetadata newEntry = new CacheServiceMetadata(url, cacheFile);
                        if (!librariesMetadata.Contains(newEntry))
                        {
                            librariesMetadata.Add(new CacheServiceMetadata(url, cacheFile));
                        }
                    }

                    await _cacheService.RefreshCacheAsync(librariesMetadata, cancellationToken);
                }
            }
            catch (ResourceDownloadException ex)
            {
                HostInteraction.Logger.Log(ex.ToString(), LogLevel.Error);
                return new LibraryOperationResult(state, PredefinedErrors.FailedToDownloadResource(ex.Url));
            }
            catch (OperationCanceledException)
            {
                return LibraryOperationResult.FromCancelled(state);
            }
            catch (Exception ex)
            {
                HostInteraction.Logger.Log(ex.InnerException.ToString(), LogLevel.Error);
                return new LibraryOperationResult(state, PredefinedErrors.UnknownException());
            }

            return LibraryOperationResult.FromSuccess(state);
        }

        private bool IsLibraryUpToDate(ILibraryInstallationState state)
        {
            try
            {
                if (!string.IsNullOrEmpty(state.Name) && !string.IsNullOrEmpty(state.Version))
                {
                    string cacheDir = Path.Combine(CacheFolder, state.Name, state.Version);
                    string destinationDir = Path.Combine(HostInteraction.WorkingDirectory, state.DestinationPath);

                    foreach (string sourceFile in state.Files)
                    {
                        var destinationFile = new FileInfo(Path.Combine(destinationDir, sourceFile).Replace('\\', '/'));
                        var cacheFile = new FileInfo(Path.Combine(cacheDir, sourceFile).Replace('\\', '/'));

                        if (!destinationFile.Exists || !cacheFile.Exists || !FileHelpers.AreFilesUpToDate(destinationFile, cacheFile))
                        {
                            return false;
                        }
                    }
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
