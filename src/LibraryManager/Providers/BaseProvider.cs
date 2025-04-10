// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Cache;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Helpers;
using Microsoft.Web.LibraryManager.LibraryNaming;
using Microsoft.Web.LibraryManager.Utilities;

namespace Microsoft.Web.LibraryManager.Providers
{
    /// <summary>
    /// Default implementation for a provider, since most provider implementations are very similar.
    /// </summary>
    internal abstract class BaseProvider : IProvider
    {
        protected readonly CacheService _cacheService;
        private string _cacheFolder;

        public BaseProvider(IHostInteraction hostInteraction, CacheService cacheService)
        {
            HostInteraction = hostInteraction;
            _cacheService = cacheService;
        }

        #region IProvider implementation

        /// <inheritdoc />
        public abstract string Id { get; }

        /// <inheritdoc />
        public virtual string NuGetPackageId => "Microsoft.Web.LibraryManager.Build";

        /// <inheritdoc />
        public abstract string LibraryIdHintText { get; }

        /// <inheritdoc />
        public IHostInteraction HostInteraction { get; }

        /// <inheritdoc />
        public virtual bool SupportsLibraryVersions => true;

        /// <inheritdoc />
        public abstract ILibraryCatalog GetCatalog();

        /// <inheritdoc />
        public abstract string GetSuggestedDestination(ILibrary library);

        /// <inheritdoc />
        public virtual async Task<OperationResult<LibraryInstallationGoalState>> InstallAsync(ILibraryInstallationState desiredState, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return OperationResult<LibraryInstallationGoalState>.FromCancelled(null);
            }

            OperationResult<ILibrary> getLibrary = await GetLibraryForInstallationState(desiredState, cancellationToken).ConfigureAwait(false);
            if (!getLibrary.Success)
            {
                return new OperationResult<LibraryInstallationGoalState>([.. getLibrary.Errors])
                {
                    Cancelled = getLibrary.Cancelled,
                };
            }

            OperationResult<LibraryInstallationGoalState> getGoalState = GenerateGoalState(desiredState, getLibrary.Result);
            if (!getGoalState.Success)
            {
                return getGoalState;
            }

            LibraryInstallationGoalState goalState = getGoalState.Result;

            if (!IsSourceCacheReady(goalState))
            {
                OperationResult<LibraryInstallationGoalState> updateCacheResult = await RefreshCacheAsync(goalState, getLibrary.Result, cancellationToken);
                if (!updateCacheResult.Success)
                {
                    return updateCacheResult;
                }
            }

            if (goalState.IsAchieved())
            {
                return OperationResult<LibraryInstallationGoalState>.FromUpToDate(goalState);
            }

            return await InstallFiles(goalState, cancellationToken);

        }

        private async Task<OperationResult<ILibrary>> GetLibraryForInstallationState(ILibraryInstallationState desiredState, CancellationToken cancellationToken)
        {
            ILibrary library;
            try
            {
                ILibraryCatalog catalog = GetCatalog();
                library = await catalog.GetLibraryAsync(desiredState.Name, desiredState.Version, cancellationToken).ConfigureAwait(false);
            }
            catch (InvalidLibraryException)
            {
                string libraryId = LibraryNamingScheme.GetLibraryId(desiredState.Name, desiredState.Version);
                return OperationResult<ILibrary>.FromError(PredefinedErrors.UnableToResolveSource(libraryId, desiredState.ProviderId));
            }
            catch (Exception ex)
            {
                HostInteraction.Logger.Log(ex.ToString(), LogLevel.Error);
                return OperationResult<ILibrary>.FromError(PredefinedErrors.UnknownException());
            }

            return OperationResult<ILibrary>.FromSuccess(library);
        }

        private async Task<OperationResult<LibraryInstallationGoalState>> InstallFiles(LibraryInstallationGoalState goalState, CancellationToken cancellationToken)
        {
            try
            {
                foreach (KeyValuePair<string, string> kvp in goalState.InstalledFiles)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return OperationResult<LibraryInstallationGoalState>.FromCancelled(goalState);
                    }

                    string sourcePath = kvp.Value;
                    string destinationPath = kvp.Key;
                    bool writeOk = await HostInteraction.CopyFileAsync(sourcePath, destinationPath, cancellationToken);

                    if (!writeOk)
                    {
                        return new OperationResult<LibraryInstallationGoalState>(goalState, PredefinedErrors.CouldNotWriteFile(destinationPath));
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return new OperationResult<LibraryInstallationGoalState>(goalState, PredefinedErrors.PathOutsideWorkingDirectory());
            }
            catch (Exception ex)
            {
                HostInteraction.Logger.Log(ex.ToString(), LogLevel.Error);
                return new OperationResult<LibraryInstallationGoalState>(goalState, PredefinedErrors.UnknownException());
            }

            return OperationResult<LibraryInstallationGoalState>.FromSuccess(goalState);
        }

        public async Task<OperationResult<LibraryInstallationGoalState>> GetInstallationGoalStateAsync(ILibraryInstallationState desiredState, CancellationToken cancellationToken)
        {
            // get the library from the catalog
            OperationResult<ILibrary> getLibrary = await GetLibraryForInstallationState(desiredState, cancellationToken).ConfigureAwait(false);
            if (!getLibrary.Success)
            {
                return OperationResult<LibraryInstallationGoalState>.FromErrors([.. getLibrary.Errors]);
            }

            return GenerateGoalState(desiredState, getLibrary.Result);
        }

        #endregion

        /// <summary>
        /// Generates the goal state for library installation based on the desired state and library information.
        /// </summary>
        /// <param name="desiredState">Specifies the target state for the library installation, including file mappings and destination paths.</param>
        /// <param name="library">Represents the library from which files are being installed, containing file information and validation
        /// methods.</param>
        /// <returns>Returns an operation result containing the goal state or errors encountered during the generation process.</returns>
        private OperationResult<LibraryInstallationGoalState> GenerateGoalState(ILibraryInstallationState desiredState, ILibrary library)
        {
            var mappings = new List<FileMapping>(desiredState.FileMappings ?? []);
            List<IError> errors = null;
            if (desiredState.Files is { Count: > 0 })
            {
                mappings.Add(new FileMapping { Destination = desiredState.DestinationPath, Files = desiredState.Files });
            }
            else if (desiredState.FileMappings is null or { Count: 0 })
            {
                // no files specified and no file mappings => include all files
                mappings.Add(new FileMapping { Destination = desiredState.DestinationPath });
            }

            Dictionary<string, string> installFiles = new(StringComparer.OrdinalIgnoreCase);

            foreach (FileMapping fileMapping in mappings)
            {
                // if Root is not specified, assume it's the root of the library
                string mappingRoot = fileMapping.Root ?? string.Empty;
                // if Destination is not specified, inherit from the library entry
                string destination = fileMapping.Destination ?? desiredState.DestinationPath;

                if (destination is null)
                {
                    errors ??= [];
                    string libraryId = LibraryNamingScheme.GetLibraryId(desiredState.Name, desiredState.Version);
                    errors.Add(PredefinedErrors.DestinationNotSpecified(libraryId));
                    continue;
                }

                IReadOnlyList<string> fileFilters;
                if (fileMapping.Files is { Count: > 0 })
                {
                    fileFilters = fileMapping.Files;
                }
                else
                {
                    fileFilters = ["**"];
                }

                if (mappingRoot.Length > 0)
                {
                    // prefix mappingRoot to each fileFilter item
                    fileFilters = fileFilters.Select(f => $"{mappingRoot}/{f}").ToList();
                }

                List<string> filteredFiles = FileGlobbingUtility.ExpandFileGlobs(fileFilters, library.Files.Keys).ToList();

                if (library.GetInvalidFiles(filteredFiles) is IReadOnlyList<string> { Count: > 0 } invalidFiles)
                {
                    errors ??= [];
                    string libraryId = LibraryNamingScheme.GetLibraryId(desiredState.Name, desiredState.Version);
                    errors.Add(PredefinedErrors.InvalidFilesInLibrary(libraryId, invalidFiles, library.Files.Keys));
                    filteredFiles.RemoveAll(file => invalidFiles.Contains(file));
                }

                Dictionary<string, string> fileMappings = GetFileMappings(library, filteredFiles, mappingRoot, destination, desiredState, errors);

                foreach ((string destinationFile, string sourceFile) in fileMappings)
                {
                    if (!FileHelpers.IsUnderRootDirectory(destinationFile, HostInteraction.WorkingDirectory))
                    {
                        errors ??= [];
                        errors.Add(PredefinedErrors.PathOutsideWorkingDirectory());
                        continue;
                    }

                    // map destination back to the library-relative file it originated from
                    if (installFiles.ContainsKey(destinationFile))
                    {
                        // this file is already being installed from another mapping
                        errors ??= [];
                        string libraryId = LibraryNamingScheme.GetLibraryId(desiredState.Name, desiredState.Version);
                        errors.Add(PredefinedErrors.LibraryCannotBeInstalledDueToConflicts(destinationFile, [libraryId]));
                        continue;
                    }

                    installFiles.Add(destinationFile, sourceFile);
                }
            }

            if (errors is not null)
            {
                return OperationResult<LibraryInstallationGoalState>.FromErrors([.. errors]);
            }

            var goalState = new LibraryInstallationGoalState(desiredState, installFiles);
            return OperationResult<LibraryInstallationGoalState>.FromSuccess(goalState);
        }


        protected virtual Dictionary<string, string> GetFileMappings(ILibrary library, IReadOnlyList<string> libraryFiles, string mappingRoot, string destination, ILibraryInstallationState desiredState, List<IError> errors)
        {
            Dictionary<string, string> installFiles = new(StringComparer.OrdinalIgnoreCase);

            foreach (string file in libraryFiles)
            {
                // strip the source prefix
                string relativeOutFile = mappingRoot.Length > 0 ? file.Substring(mappingRoot.Length + 1) : file;
                string destinationFile = Path.Combine(HostInteraction.WorkingDirectory, destination, relativeOutFile);
                destinationFile = FileHelpers.NormalizePath(destinationFile);

                // include the cache folder in the path
                string sourceFile = GetCachedFileLocalPath(desiredState, file);
                sourceFile = FileHelpers.NormalizePath(sourceFile);

                installFiles.Add(destinationFile, sourceFile);
            }

            return installFiles;
        }

        public bool IsSourceCacheReady(LibraryInstallationGoalState goalState)
        {
            foreach (KeyValuePair<string, string> item in goalState.InstalledFiles)
            {
                string cachePath = GetCachedFileLocalPath(goalState.InstallationState, item.Value);
                // TODO: use abstraction for filesystem ops
                if (!File.Exists(cachePath))
                {
                    return false;
                }
            }

            return true;
        }

        protected virtual ILibraryNamingScheme LibraryNamingScheme { get; } = new VersionedLibraryNamingScheme();

        public string CacheFolder
        {
            get { return _cacheFolder ?? (_cacheFolder = Path.Combine(HostInteraction.CacheDirectory, Id)); }
        }

        /// <summary>
        /// Gets the expected local path for a file from the file cache
        /// </summary>
        /// <returns></returns>
        protected virtual string GetCachedFileLocalPath(ILibraryInstallationState state, string sourceFile)
        {
            return Path.Combine(CacheFolder, state.Name, state.Version, sourceFile.Trim('/'));
        }

        /// <summary>
        /// Copies ILibraryInstallationState files to cache
        /// </summary>
        /// <param name="state">Desired install state to cache</param>
        /// <param name="library">Library resolved from provider</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<OperationResult<LibraryInstallationGoalState>> RefreshCacheAsync(LibraryInstallationGoalState goalState, ILibrary library, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return OperationResult<LibraryInstallationGoalState>.FromCancelled(goalState);
            }

            string libraryDir = Path.Combine(CacheFolder, goalState.InstallationState.Name, goalState.InstallationState.Version);

            try
            {
                IEnumerable<string> filesToCache;
                // expand "files" to concrete files in the library


                // TODO: where do we do FileMappings?
                if (goalState.InstallationState.Files == null || goalState.InstallationState.Files.Count == 0)
                {
                    filesToCache = library.Files.Keys;
                }
                else
                {
                    filesToCache = FileGlobbingUtility.ExpandFileGlobs(goalState.InstallationState.Files, library.Files.Keys);
                }

                var librariesMetadata = new HashSet<CacheFileMetadata>();
                foreach (string sourceFile in filesToCache)
                {
                    string cacheFile = Path.Combine(libraryDir, sourceFile);
                    string url = GetDownloadUrl(goalState.InstallationState, sourceFile);

                    var newEntry = new CacheFileMetadata(url, cacheFile);
                    librariesMetadata.Add(newEntry);
                }
                await _cacheService.RefreshCacheAsync(librariesMetadata, HostInteraction.Logger, cancellationToken);
            }
            catch (ResourceDownloadException ex)
            {
                HostInteraction.Logger.Log(ex.ToString(), LogLevel.Error);
                return new OperationResult<LibraryInstallationGoalState>(goalState, PredefinedErrors.FailedToDownloadResource(ex.Url));
            }
            catch (OperationCanceledException)
            {
                return OperationResult<LibraryInstallationGoalState>.FromCancelled(goalState);
            }
            catch (Exception ex)
            {
                HostInteraction.Logger.Log(ex.InnerException.ToString(), LogLevel.Error);
                return new OperationResult<LibraryInstallationGoalState>(goalState, PredefinedErrors.UnknownException());
            }

            return OperationResult<LibraryInstallationGoalState>.FromSuccess(goalState);
        }

        protected abstract string GetDownloadUrl(ILibraryInstallationState state, string sourceFile);
    }
}
