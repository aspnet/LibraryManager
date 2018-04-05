// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryManager.Contracts;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Web.LibraryManager.Providers.FileSystem
{
    /// <summary>Internal use only</summary>
    internal class FileSystemProvider : IProvider
    {
        /// <summary>Internal use only</summary>
        public FileSystemProvider(IHostInteraction hostInteraction)
        {
            HostInteraction = hostInteraction;
        }

        /// <summary>
        /// The unique identifier of the provider.
        /// </summary>
        public string Id { get; } = "filesystem";

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

        /// <summary>
        /// Gets the <see cref="T:Microsoft.Web.LibraryManager.Contracts.ILibraryCatalog" /> for the <see cref="T:Microsoft.Web.LibraryManager.Contracts.IProvider" />. May be <code>null</code> if no catalog is supported.
        /// </summary>
        /// <returns></returns>
        public ILibraryCatalog GetCatalog()
        {
            return new FileSystemCatalog(this);
        }

        /// <summary>
        /// Installs a library as specified in the <paramref name="desiredState" /> parameter.
        /// </summary>
        /// <param name="desiredState">The details about the library to install.</param>
        /// <param name="cancellationToken">A token that allows for the operation to be cancelled.</param>
        /// <returns>
        /// The <see cref="T:Microsoft.Web.LibraryManager.Contracts.ILibraryInstallationResult" /> from the installation process.
        /// </returns>
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

            try
            {
                ILibraryInstallationResult result = await UpdateStateAsync(desiredState, cancellationToken);

                if (!result.Success)
                {
                    return result;
                }

                desiredState = result.InstallationState;

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

        private async Task<Stream> GetStreamAsync(ILibraryInstallationState state, string file, CancellationToken cancellationToken)
        {
            string sourceFile = state.LibraryId;

            try
            {
                if (!Uri.TryCreate(sourceFile, UriKind.RelativeOrAbsolute, out Uri url))
                    return null;

                if (!url.IsAbsoluteUri)
                {
                    sourceFile = new FileInfo(Path.Combine(HostInteraction.WorkingDirectory, sourceFile)).FullName;
                    if (!Uri.TryCreate(sourceFile, UriKind.Absolute, out url))
                        return null;
                }

                // File
                if (url.IsFile)
                {
                    if (Directory.Exists(url.OriginalString))
                    {
                        return await FileHelpers.OpenFileAsync(Path.Combine(url.OriginalString, file), cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        return await FileHelpers.OpenFileAsync(sourceFile, cancellationToken).ConfigureAwait(false);
                    }
                }
                // Url
                else
                {
                    var client = new HttpClient();
                    return await client.GetStreamAsync(sourceFile).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                throw new InvalidLibraryException(state.LibraryId, state.ProviderId);
            }
        }
    }
}