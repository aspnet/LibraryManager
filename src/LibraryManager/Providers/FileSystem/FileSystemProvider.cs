// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Resources;

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
        /// Hint text for the library id.
        /// </summary>
        public string LibraryIdHintText { get; } = Text.FileSystemLibraryIdHintText;

        /// <summary>
        /// Does not support libraries with versions.
        /// </summary>
        public bool SupportsLibraryVersions => false;

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
        /// The <see cref="T:Microsoft.Web.LibraryManager.Contracts.ILibraryOperationResult" /> from the installation process.
        /// </returns>
        public async Task<ILibraryOperationResult> InstallAsync(ILibraryInstallationState desiredState, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return LibraryOperationResult.FromCancelled(desiredState);
            }

            try
            {
                ILibraryOperationResult result = await UpdateStateAsync(desiredState, cancellationToken);

                if (!result.Success)
                {
                    return result;
                }

                desiredState = result.InstallationState;

                foreach (string file in desiredState.Files)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return LibraryOperationResult.FromCancelled(desiredState);
                    }

                    if (string.IsNullOrEmpty(file))
                    {
                        return new LibraryOperationResult(desiredState, PredefinedErrors.CouldNotWriteFile(file));
                    }

                    string path = Path.Combine(desiredState.DestinationPath, file);
                    var sourceStream = new Func<Stream>(() => GetStreamAsync(desiredState, file, cancellationToken).Result);
                    bool writeOk = await HostInteraction.WriteFileAsync(path, sourceStream, desiredState, cancellationToken).ConfigureAwait(false);

                    if (!writeOk)
                    {
                        return new LibraryOperationResult(desiredState, PredefinedErrors.CouldNotWriteFile(file));
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return new LibraryOperationResult(desiredState, PredefinedErrors.PathOutsideWorkingDirectory());
            }
            catch (ResourceDownloadException ex)
            {
                return new LibraryOperationResult(desiredState, PredefinedErrors.FailedToDownloadResource(ex.Url));
            }
            catch (Exception ex)
            {
                HostInteraction.Logger.Log(ex.ToString(), LogLevel.Error);
                return new LibraryOperationResult(desiredState, PredefinedErrors.UnknownException());
            }

            return LibraryOperationResult.FromSuccess(desiredState);
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
                ILibrary library = await catalog.GetLibraryAsync(desiredState.Name, desiredState.Version, cancellationToken).ConfigureAwait(false);

                if (library == null)
                {
                    return new LibraryOperationResult(desiredState, PredefinedErrors.UnableToResolveSource(desiredState.Name, Id));
                }

                if (desiredState.Files != null && desiredState.Files.Count > 0)
                {
                    return LibraryOperationResult.FromSuccess(desiredState);
                }

                desiredState = new LibraryInstallationState
                {
                    ProviderId = Id,
                    Name = desiredState.Name,
                    DestinationPath = desiredState.DestinationPath,
                    Files = library.Files.Keys.ToList(),
                    IsUsingDefaultDestination = desiredState.IsUsingDefaultDestination,
                    IsUsingDefaultProvider = desiredState.IsUsingDefaultProvider
                };
            }
            catch (InvalidLibraryException)
            {
                return new LibraryOperationResult(desiredState, PredefinedErrors.UnableToResolveSource(desiredState.Name, desiredState.ProviderId));
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

        /// <summary>
        /// Returns the last valid filename part of the path which identifies the library.
        /// </summary>
        /// <param name="library"></param>
        /// <returns></returns>
        public string GetSuggestedDestination(ILibrary library)
        {
            if (library != null && library is FileSystemLibrary fileSystemLibrary)
            {
                char[] invalidPathChars = Path.GetInvalidFileNameChars();
                string name = fileSystemLibrary.Name.TrimEnd(invalidPathChars);
                int invalidCharIndex = name.LastIndexOfAny(invalidPathChars);
                if (invalidCharIndex > 0)
                {
                    name = name.Substring(invalidCharIndex + 1);
                }

                return Path.GetFileNameWithoutExtension(name);
            }

            return string.Empty;
        }

        private async Task<Stream> GetStreamAsync(ILibraryInstallationState state, string file, CancellationToken cancellationToken)
        {
            string sourceFile = state.Name;

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
                        return await FileHelpers.ReadFileAsStreamAsync(Path.Combine(url.OriginalString, file), cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        return await FileHelpers.ReadFileAsStreamAsync(sourceFile, cancellationToken).ConfigureAwait(false);
                    }
                }
                // Url
                else
                {
                    return await GetRemoteResourceAsync(sourceFile);
                }
            }
            catch (ResourceDownloadException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new InvalidLibraryException(state.Name, state.ProviderId);
            }
        }

        private static async Task<Stream> GetRemoteResourceAsync(string sourceUrl)
        {
            try
            {
                var client = new HttpClient();
                return await client.GetStreamAsync(sourceUrl).ConfigureAwait(false);
            }
            catch (Exception)
            {
                throw new ResourceDownloadException(sourceUrl);
            }
        }
    }
}
