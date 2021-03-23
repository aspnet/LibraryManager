// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.LibraryNaming;
using Microsoft.Web.LibraryManager.Resources;

namespace Microsoft.Web.LibraryManager.Providers.FileSystem
{
    /// <summary>Internal use only</summary>
    internal sealed class FileSystemProvider : BaseProvider
    {
        private FileSystemCatalog _catalog;

        /// <summary>Internal use only</summary>
        public FileSystemProvider(IHostInteraction hostInteraction)
            :base(hostInteraction, null)
        {
        }

        /// <summary>
        /// The unique identifier of the provider.
        /// </summary>
        public override string Id => "filesystem";

        /// <summary>
        /// Hint text for the library id.
        /// </summary>
        public override string LibraryIdHintText => Text.FileSystemLibraryIdHintText;

        /// <summary>
        /// Does not support libraries with versions.
        /// </summary>
        public override bool SupportsLibraryVersions => false;

        /// <summary>
        /// Gets the <see cref="Microsoft.Web.LibraryManager.Contracts.ILibraryCatalog" /> for the <see cref="Microsoft.Web.LibraryManager.Contracts.IProvider" />. May be <code>null</code> if no catalog is supported.
        /// </summary>
        /// <returns></returns>
        public override ILibraryCatalog GetCatalog()
        {
            return _catalog ?? (_catalog = new FileSystemCatalog(this));
        }

        /// <summary>
        /// Installs a library as specified in the <paramref name="desiredState" /> parameter.
        /// </summary>
        /// <param name="desiredState">The details about the library to install.</param>
        /// <param name="cancellationToken">A token that allows for the operation to be cancelled.</param>
        /// <returns>
        /// The <see cref="Microsoft.Web.LibraryManager.Contracts.ILibraryOperationResult" /> from the installation process.
        /// </returns>
        public override async Task<ILibraryOperationResult> InstallAsync(ILibraryInstallationState desiredState, CancellationToken cancellationToken)
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

        protected override ILibraryOperationResult CheckForInvalidFiles(ILibraryInstallationState desiredState, string libraryId, ILibrary library)
        {
            return LibraryOperationResult.FromSuccess(desiredState);
        }

        /// <summary>
        /// Returns the last valid filename part of the path which identifies the library.
        /// </summary>
        /// <param name="library"></param>
        /// <returns></returns>
        public override string GetSuggestedDestination(ILibrary library)
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
#pragma warning disable CA2000 // Dispose objects before losing scope
                var client = new HttpClient();
#pragma warning restore CA2000 // Dispose objects before losing scope
                return await client.GetStreamAsync(new Uri(sourceUrl)).ConfigureAwait(false);
            }
            catch (Exception)
            {
                throw new ResourceDownloadException(sourceUrl);
            }
        }

        protected override ILibraryNamingScheme LibraryNamingScheme { get; } = new SimpleLibraryNamingScheme();

        protected override string GetDownloadUrl(ILibraryInstallationState state, string sourceFile)
        {
            throw new NotSupportedException();
        }
    }
}
