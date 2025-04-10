// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Helpers;
using Microsoft.Web.LibraryManager.LibraryNaming;
using Microsoft.Web.LibraryManager.Resources;

namespace Microsoft.Web.LibraryManager.Providers.FileSystem
{
    /// <summary>Internal use only</summary>
    internal sealed class FileSystemProvider : BaseProvider
    {
        public const string IdText = "filesystem";
        private FileSystemCatalog _catalog;

        /// <summary>Internal use only</summary>
        public FileSystemProvider(IHostInteraction hostInteraction)
            :base(hostInteraction, null)
        {
        }

        /// <summary>
        /// The unique identifier of the provider.
        /// </summary>
        public override string Id => IdText;

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
        /// The <see cref="OperationResult{LibraryInstallationGoalState}" /> from the installation process.
        /// </returns>
        public override async Task<OperationResult<LibraryInstallationGoalState>> InstallAsync(ILibraryInstallationState desiredState, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return OperationResult<LibraryInstallationGoalState>.FromCancelled(null);
            }

            try
            {
                OperationResult<LibraryInstallationGoalState> goalStateResult = await GetInstallationGoalStateAsync(desiredState, cancellationToken).ConfigureAwait(false);
                if (!goalStateResult.Success)
                {
                    return goalStateResult;
                }

                foreach ((string destFile, string sourceFile) in goalStateResult.Result.InstalledFiles)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return OperationResult<LibraryInstallationGoalState>.FromCancelled(goalStateResult.Result);
                    }

                    if (string.IsNullOrEmpty(destFile))
                    {
                        return OperationResult<LibraryInstallationGoalState>.FromError(PredefinedErrors.CouldNotWriteFile(destFile));
                    }

                    string libraryName = LibraryNamingScheme.GetLibraryId(desiredState.Name, desiredState.Version);
                    var sourceStream = new Func<Stream>(() => GetStreamAsync(sourceFile, libraryName, cancellationToken).Result);
                    bool writeOk = await HostInteraction.WriteFileAsync(destFile, sourceStream, desiredState, cancellationToken).ConfigureAwait(false);

                    if (!writeOk)
                    {
                        return OperationResult<LibraryInstallationGoalState>.FromError(PredefinedErrors.CouldNotWriteFile(destFile));
                    }
                }

                return OperationResult<LibraryInstallationGoalState>.FromSuccess(goalStateResult.Result);
            }
            catch (UnauthorizedAccessException)
            {
                return OperationResult<LibraryInstallationGoalState>.FromError(PredefinedErrors.PathOutsideWorkingDirectory());
            }
            catch (ResourceDownloadException ex)
            {
                return OperationResult<LibraryInstallationGoalState>.FromError(PredefinedErrors.FailedToDownloadResource(ex.Url));
            }
            catch (Exception ex)
            {
                HostInteraction.Logger.Log(ex.ToString(), LogLevel.Error);
                return OperationResult<LibraryInstallationGoalState>.FromError(PredefinedErrors.UnknownException());
            }
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

        private async Task<Stream> GetStreamAsync(string sourceFile, string libraryName, CancellationToken cancellationToken)
        {
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
                    return await FileHelpers.ReadFileAsStreamAsync(sourceFile, cancellationToken).ConfigureAwait(false);
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
                throw new InvalidLibraryException(libraryName, Id);
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

        protected override string GetCachedFileLocalPath(ILibraryInstallationState state, string sourceFile)
        {
            // FileSystemProvider pulls files directly, no caching.  So here we need to build a full
            // path or URI to the file.

            // For HTTP files, the state.Name is the full URL to a single file
            if (FileHelpers.IsHttpUri(state.Name))
            {
                return state.Name;
            }

            // For other filesystem libraries, the state.Name may be a either a file or folder
            // TODO: abstract file system
            (bool isFile, string resolvedFilePath) = LibraryNameIsFile(state.Name);
            
            if (isFile)
            {
                return resolvedFilePath;
            }

            // as a fallback, assume state.Name is a directory.  If this path doesn't exist, it will
            // be handled elsewhere.

            // root relative paths to the libman working directory
            if (!Path.IsPathRooted(state.Name))
            {
                return Path.GetFullPath(Path.Combine(HostInteraction.WorkingDirectory, state.Name, sourceFile));
            }

            return Path.Combine(state.Name, sourceFile);
        }

        /// <inheritdoc />
        protected override Dictionary<string, string> GetFileMappings(ILibrary library, IReadOnlyList<string> fileFilters, string mappingRoot, string destination, ILibraryInstallationState desiredState, List<IError> errors)
        {
            Dictionary<string, string> fileMappings = new();
            // Handle single-file edge cases for FileSystem
            (bool librarySpecifiedIsFile, string resolvedFilePath) = LibraryNameIsFile(library.Name);
            if (librarySpecifiedIsFile && fileFilters.Count == 1)
            {
                // direct 1:1 file mapping, allowing file rename
                string destinationFile = Path.Combine(HostInteraction.WorkingDirectory, destination, fileFilters[0]);
                destinationFile = FileHelpers.NormalizePath(destinationFile);

                // the library specified is a single file, so use that as the source directly
                fileMappings.Add(destinationFile, resolvedFilePath);
                return fileMappings;
            }

            return base.GetFileMappings(library, fileFilters, mappingRoot, destination, desiredState, errors);
        }

        /// <summary>
        /// Checks if a specified library name corresponds to an existing file and returns the result along with the
        /// file path.
        /// </summary>
        /// <param name="libraryName">The name of the library being checked for existence as a file.</param>
        /// <returns>A tuple containing a boolean indicating if the file exists and the resolved file path.</returns>
        private (bool, string) LibraryNameIsFile(string libraryName)
        {
            string filePath = libraryName;
            if (FileHelpers.IsHttpUri(filePath))
            {
                return (true, filePath);
            }

            if (!Path.IsPathRooted(filePath))
            {
                filePath = Path.Combine(HostInteraction.WorkingDirectory, filePath);
            }

            return (File.Exists(filePath), filePath);
        }
    }
}
