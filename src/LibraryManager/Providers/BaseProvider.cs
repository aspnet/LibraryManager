// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
    /// Default implenentation for a provider, since most provider implementations are very similar.
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
        public virtual async Task<ILibraryOperationResult> InstallAsync(ILibraryInstallationState desiredState, CancellationToken cancellationToken)
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

            // Check if Library is already up to date
            if (IsLibraryUpToDate(desiredState))
            {
                return LibraryOperationResult.FromUpToDate(desiredState);
            }

            // Write files to destination
            return await WriteToFilesAsync(desiredState, cancellationToken);
        }

        /// <inheritdoc />
        public virtual async Task<ILibraryOperationResult> UpdateStateAsync(ILibraryInstallationState desiredState, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return LibraryOperationResult.FromCancelled(desiredState);
            }

            string libraryId = LibraryNamingScheme.GetLibraryId(desiredState.Name, desiredState.Version);
            try
            {
                ILibraryCatalog catalog = GetCatalog();
                ILibrary library = await catalog.GetLibraryAsync(desiredState.Name, desiredState.Version, cancellationToken).ConfigureAwait(false);

                if (library == null)
                {
                    return new LibraryOperationResult(desiredState, PredefinedErrors.UnableToResolveSource(desiredState.Name, desiredState.ProviderId));
                }

                if (desiredState.Files != null && desiredState.Files.Count > 0)
                {
                    // expand any potential file patterns
                    IEnumerable<string> updatedFiles = FileGlobbingUtility.ExpandFileGlobs(desiredState.Files, library.Files.Keys);
                    var processedState = new LibraryInstallationState
                    {
                        Name = desiredState.Name,
                        Version = desiredState.Version,
                        ProviderId = desiredState.ProviderId,
                        DestinationPath = desiredState.DestinationPath,
                        IsUsingDefaultDestination = desiredState.IsUsingDefaultDestination,
                        IsUsingDefaultProvider = desiredState.IsUsingDefaultProvider,
                        Files = updatedFiles.ToList(),
                    };

                    return CheckForInvalidFiles(processedState, libraryId, library);
                }

                desiredState = new LibraryInstallationState
                {
                    ProviderId = Id,
                    Name = desiredState.Name,
                    Version = desiredState.Version,
                    DestinationPath = desiredState.DestinationPath,
                    Files = library.Files.Keys.ToList(),
                    IsUsingDefaultDestination = desiredState.IsUsingDefaultDestination,
                    IsUsingDefaultProvider = desiredState.IsUsingDefaultProvider
                };
            }
            catch (InvalidLibraryException)
            {
                return new LibraryOperationResult(desiredState, PredefinedErrors.UnableToResolveSource(libraryId, desiredState.ProviderId));
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

        #endregion

        protected virtual ILibraryOperationResult CheckForInvalidFiles(ILibraryInstallationState desiredState, string libraryId, ILibrary library)
        {
            IReadOnlyList<string> invalidFiles = library.GetInvalidFiles(desiredState.Files);
            if (invalidFiles.Count > 0)
            {
                IError invalidFilesError = PredefinedErrors.InvalidFilesInLibrary(libraryId, invalidFiles, library.Files.Keys);
                return new LibraryOperationResult(desiredState, invalidFilesError);
            }
            else
            {
                return LibraryOperationResult.FromSuccess(desiredState);
            }
        }

        protected virtual ILibraryNamingScheme LibraryNamingScheme { get; } = new VersionedLibraryNamingScheme();

        public string CacheFolder
        {
            get { return _cacheFolder ?? (_cacheFolder = Path.Combine(HostInteraction.CacheDirectory, Id)); }
        }

        /// <summary>
        /// Copy files from the download cache to the desired installation state
        /// </summary>
        /// <remarks>Precondition: all files must already exist in the cache</remarks>
        protected async Task<ILibraryOperationResult> WriteToFilesAsync(ILibraryInstallationState state, CancellationToken cancellationToken)
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
                            string id = LibraryNamingScheme.GetLibraryId(state.Name, state.Version);
                            return new LibraryOperationResult(state, PredefinedErrors.FileNameMustNotBeEmpty(id));
                        }

                        string sourcePath = GetCachedFileLocalPath(state, file);
                        string destinationPath = Path.Combine(state.DestinationPath, file);
                        bool writeOk = await HostInteraction.CopyFileAsync(sourcePath, destinationPath, cancellationToken);

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
        /// Gets the expected local path for a file from the file cache
        /// </summary>
        /// <returns></returns>
        private string GetCachedFileLocalPath(ILibraryInstallationState state, string sourceFile)
        {
            return Path.Combine(CacheFolder, state.Name, state.Version, sourceFile);
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

            string libraryDir = Path.Combine(CacheFolder, state.Name, state.Version);

            try
            {
                var librariesMetadata = new HashSet<CacheFileMetadata>();
                foreach (string sourceFile in state.Files)
                {
                    string cacheFile = Path.Combine(libraryDir, sourceFile);
                    string url = GetDownloadUrl(state, sourceFile);

                    var newEntry = new CacheFileMetadata(url, cacheFile);
                    librariesMetadata.Add(newEntry);
                }
                await _cacheService.RefreshCacheAsync(librariesMetadata, HostInteraction.Logger, cancellationToken);
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

        protected abstract string GetDownloadUrl(ILibraryInstallationState state, string sourceFile);
    }
}
