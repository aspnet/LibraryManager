// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;
using Newtonsoft.Json;

namespace Microsoft.Web.LibraryManager
{
    /// <summary>
    /// Represents the manifest JSON file and orchestrates the interaction
    /// with the various <see cref="IProvider"/> instances.
    /// </summary>
    public class Manifest
    {
        /// <summary>
        /// Supported versions of Library Manager
        /// </summary>
        public static readonly Version[] SupportedVersions = { new Version("1.0") };
        private IHostInteraction _hostInteraction;
        private readonly List<ILibraryInstallationState> _libraries;
        private IDependencies _dependencies;

        /// <summary>
        /// Creates a new instance of <see cref="Manifest"/>.
        /// </summary>
        /// <param name="dependencies">The host provided dependencies.</param>
        public Manifest(IDependencies dependencies)
        {
            _libraries = new List<ILibraryInstallationState>();
            _dependencies = dependencies;
            _hostInteraction = dependencies?.GetHostInteractions();
        }

        /// <summary>
        /// The version of the <see cref="Manifest"/> document format.
        /// </summary>
        [JsonProperty(ManifestConstants.Version)]
        public string Version { get; set; }

        /// <summary>
        /// The default <see cref="Manifest"/> library provider.
        /// </summary>
        [JsonProperty(ManifestConstants.DefaultProvider)]
        public string DefaultProvider { get; set; }

        /// <summary>
        /// The default destination path for libraries.
        /// </summary>
        [JsonProperty(ManifestConstants.DefaultDestination)]
        public string DefaultDestination { get; set; }

        /// <summary>
        /// A list of libraries contained in the <see cref="Manifest"/>.
        /// </summary>
        [JsonProperty(ManifestConstants.Libraries)]
        [JsonConverter(typeof(LibraryStateTypeConverter))]
        public IEnumerable<ILibraryInstallationState> Libraries => _libraries;

        /// <summary>
        /// Creates a new instance of a <see cref="Manifest"/> class from a file on disk.
        /// </summary>
        /// <remarks>
        /// The <paramref name="fileName"/> doesn't have to exist on disk. It will be created when
        /// <see cref="SaveAsync"/> is invoked.
        /// </remarks>
        /// <param name="fileName">The absolute file path to the manifest JSON file.</param>
        /// <param name="dependencies">The host provided dependencies.</param>
        /// <param name="cancellationToken">A token that allows for cancellation of the operation.</param>
        /// <returns>An instance of the <see cref="Manifest"/> class.</returns>
        public static async Task<Manifest> FromFileAsync(string fileName, IDependencies dependencies, CancellationToken cancellationToken)
        {
            if (File.Exists(fileName))
            {
                string json = await FileHelpers.ReadFileAsTextAsync(fileName, cancellationToken).ConfigureAwait(false);
                return FromJson(json, dependencies);
            }

            return FromJson("{}", dependencies);
        }

        /// <summary>
        /// Creates an instance of the <see cref="Manifest"/> class based on
        /// the provided JSON string.
        /// </summary>
        /// <param name="json">A string of JSON in the correct format.</param>
        /// <param name="dependencies">The host provided dependencies.</param>
        /// <returns></returns>
        public static Manifest FromJson(string json, IDependencies dependencies)
        {
            try
            {
                Manifest manifest = JsonConvert.DeserializeObject<Manifest>(json);
                manifest._dependencies = dependencies;
                manifest._hostInteraction = dependencies.GetHostInteractions();

                UpdateLibraryProviderAndDestination(manifest);

                return manifest;
            }
            catch (Exception)
            {
                dependencies.GetHostInteractions().Logger.Log(PredefinedErrors.ManifestMalformed().Message, LogLevel.Task);
                return null;
            }
        }

        /// <summary>
        /// Removes the library from the <see cref="Libraries"/>
        /// </summary>
        /// <param name="libraryToUpdate"></param>
        /// <param name="newlibraryId"></param>
        public void ReplaceLibraryId(ILibraryInstallationState libraryToUpdate, string newlibraryId)
        {
            if (libraryToUpdate != null && libraryToUpdate is LibraryInstallationState state)
            {
                state.LibraryId = newlibraryId;
            }
        }

        private static void UpdateLibraryProviderAndDestination(Manifest manifest)
        {
            foreach (LibraryInstallationState state in manifest.Libraries.Cast<LibraryInstallationState>())
            {
                UpdateLibraryProviderAndDestination(state, manifest.DefaultProvider, manifest.DefaultDestination);
            }
        }

        private static void UpdateLibraryProviderAndDestination(ILibraryInstallationState state, string defaultProvider, string defaultDestination)
        {
            LibraryInstallationState libraryState = state as LibraryInstallationState;

            if (libraryState == null)
            {
                return;
            }

            if (state.ProviderId == null)
            {
                libraryState.ProviderId = defaultProvider;
                libraryState.IsUsingDefaultProvider = true;
            }

            if (libraryState.DestinationPath == null)
            {
                libraryState.DestinationPath = defaultDestination;
                libraryState.IsUsingDefaultDestination = true;
            }
        }

        private static bool IsValidManifestVersion(string version)
        {
            try
            {
                return SupportedVersions.Contains(new Version(version));
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a deep copy of the manifest.
        /// </summary>
        /// <returns></returns>
        public Manifest Clone()
        {
            var manifest = new Manifest(_dependencies)
            {
                Version = Version
            };

            foreach (LibraryInstallationState lib in _libraries.Cast<LibraryInstallationState>())
            {
                var newState = new LibraryInstallationState()
                {
                    LibraryId = lib.LibraryId,
                    DestinationPath = lib.DestinationPath,
                    Files = lib.Files == null ? null : new List<string>(lib.Files),
                    ProviderId = lib.ProviderId,
                    IsUsingDefaultDestination = lib.IsUsingDefaultDestination,
                    IsUsingDefaultProvider = lib.IsUsingDefaultProvider
                };

                manifest._libraries.Add(newState);
            }

            return manifest;
        }

        /// <summary>
        /// Installs a library with the given libraryId
        /// </summary>
        /// <param name="libraryId"></param>
        /// <param name="providerId"></param>
        /// <param name="files"></param>
        /// <param name="destination"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ILibraryInstallationResult>> InstallLibraryAsync(string libraryId, string providerId, IReadOnlyList<string> files, string destination, CancellationToken cancellationToken)
        {
            ILibraryInstallationResult result;

            var desiredState = new LibraryInstallationState()
            {
                LibraryId = libraryId,
                Files = files,
                ProviderId = providerId,
                DestinationPath = destination
            };

            UpdateLibraryProviderAndDestination(desiredState, DefaultProvider, DefaultDestination);
            if (!desiredState.IsValid(out IEnumerable<IError> errors))
            {
                return new List<ILibraryInstallationResult> { new LibraryInstallationResult(desiredState, errors.ToArray()) };
            }

            IProvider provider = _dependencies.GetProvider(desiredState.ProviderId);
            if (provider == null)
            {
                return new List<ILibraryInstallationResult> { new LibraryInstallationResult(desiredState, new IError[] { PredefinedErrors.ProviderUnknown(desiredState.ProviderId) })};
            }

            IEnumerable<ILibraryInstallationResult> conflictResults = await CheckLibraryForConflictsAsync(desiredState, cancellationToken);

            if (!conflictResults.All(r => r.Success))
            {
                return conflictResults;
            }

            result = await provider.InstallAsync(desiredState, cancellationToken);

            if (result.Success)
            {
                _libraries.Add(desiredState);
            }

            return new List<ILibraryInstallationResult> { result };
        }

        private async Task<IEnumerable<ILibraryInstallationResult>> CheckLibraryForConflictsAsync(ILibraryInstallationState desiredState, CancellationToken cancellationToken)
        {
            var libraries = new List<ILibraryInstallationState>(Libraries);
            libraries.Add(desiredState);
            var conflictDetector = new LibrariesValidator(_dependencies, DefaultDestination, DefaultProvider);

            IEnumerable<ILibraryInstallationResult> fileConflicts = await conflictDetector.GetLibrariesErrorsAsync(libraries, cancellationToken);

            return fileConflicts;
        }

        /// <summary>
        /// Adds a library to the <see cref="Libraries"/> collection.
        /// </summary>
        /// <param name="state">An instance of <see cref="ILibraryInstallationState"/> representing the library to add.</param>
        internal void AddLibrary(ILibraryInstallationState state)
        {
            ILibraryInstallationState existing = _libraries.Find(p => p.LibraryId == state.LibraryId && p.ProviderId == state.ProviderId);

            if (existing != null)
                _libraries.Remove(existing);

            _libraries.Add(state);
        }

        /// <summary>
        /// Adds a version to the manifest
        /// </summary>
        /// <param name="version"></param>
        public void AddVersion(string version)
        {
            Version = version;
        }

        /// <summary>
        /// Restores all libraries in the <see cref="Libraries"/> collection.
        /// </summary>
        /// <param name="cancellationToken">A token that allows for cancellation of the operation.</param>
        public async Task<IEnumerable<ILibraryInstallationResult>> RestoreAsync(CancellationToken cancellationToken)
        {
            //TODO: This should have an "undo scope"
            var tasks = new List<Task<ILibraryInstallationResult>>();

            if (!IsValidManifestVersion(Version))
            {
                return new ILibraryInstallationResult[] { LibraryInstallationResult.FromError(PredefinedErrors.VersionIsNotSupported(Version)) };
            }

            IEnumerable<ILibraryInstallationResult> validationResults = await ValidateLibrariesAsync(cancellationToken);
            if (!validationResults.All(r => r.Success))
            {
                return validationResults;
            }

            foreach (ILibraryInstallationState state in Libraries)
            {
                tasks.Add(RestoreLibraryAsync(state, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return tasks.Select(t => t.Result);
        }

        /// <summary>
        /// // Validates each individual library and check for invalid properties and libraries conflicts
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<IEnumerable<ILibraryInstallationResult>> ValidateLibrariesAsync(CancellationToken cancellationToken)
        {
            var conflictDetector = new LibrariesValidator(_dependencies, DefaultDestination, DefaultProvider);

            return await conflictDetector.GetLibrariesErrorsAsync(Libraries, cancellationToken);
        }

        private async Task<ILibraryInstallationResult> RestoreLibraryAsync(ILibraryInstallationState libraryState, CancellationToken cancellationToken)
        {
            _hostInteraction.Logger.Log(string.Format(Resources.Text.Restore_RestoreOfLibraryStarted, libraryState.LibraryId, libraryState.DestinationPath), LogLevel.Operation);

            if (cancellationToken.IsCancellationRequested)
            {
                return LibraryInstallationResult.FromCancelled(libraryState);
            }

            IProvider provider = _dependencies.GetProvider(libraryState.ProviderId);
            if (provider == null)
            {
                return new LibraryInstallationResult(libraryState, new IError[] { PredefinedErrors.ProviderUnknown(libraryState.ProviderId) });
            }

            return await provider.InstallAsync(libraryState, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Uninstalls the specified library and removes it from the <see cref="Libraries"/> collection.
        /// </summary>
        /// <param name="libraryId">The library identifier.</param>
        /// <param name="deleteFilesFunction"></param>
        /// <param name="cancellationToken"></param>
        public async Task<ILibraryInstallationResult> UninstallAsync(string libraryId, Func<IEnumerable<string>, Task<bool>> deleteFilesFunction, CancellationToken cancellationToken)
        {
            ILibraryInstallationState state = Libraries.FirstOrDefault(l => l.LibraryId == libraryId);

            if (cancellationToken.IsCancellationRequested)
            {
                return LibraryInstallationResult.FromCancelled(state);
            }

            ILibraryInstallationResult result = LibraryInstallationResult.FromError(PredefinedErrors.CouldNotDeleteLibrary(state.LibraryId));

            if (state != null)
            {
                result = await DeleteLibraryFilesAsync(state, deleteFilesFunction, cancellationToken);

                if (result.Success)
                {
                    _libraries.Remove(state);
                }
            }

            return result;
        }

        /// <summary>
        /// Uninstalls the specified library and removes it from the <see cref="Libraries"/> collection.
        /// </summary>
        /// <param name="libraryToUninstall">Provider id</param>
        /// <param name="deleteFileAction"></param>
        /// <param name="cancellationToken"></param>
        public async Task<ILibraryInstallationResult> UninstallAsync(ILibraryInstallationState libraryToUninstall, Func<IEnumerable<string>, Task<bool>> deleteFileAction, CancellationToken cancellationToken)
        {
            if (libraryToUninstall != null)
            {
                ILibraryInstallationResult result = await DeleteLibraryFilesAsync(libraryToUninstall, deleteFileAction, cancellationToken);
                if (result.Success)
                {
                    _libraries.Remove(libraryToUninstall);
                }

                return result;
            }

            return LibraryInstallationResult.FromError(PredefinedErrors.LibraryIdIsUndefined());
        }

        /// <summary>
        /// Saves the manifest file to disk.
        /// </summary>
        /// <param name="fileName">The absolute file path to save the <see cref="Manifest"/> to.</param>
        /// <param name="cancellationToken">A token that allows for cancellation of the operation.</param>
        /// <returns></returns>
        public async Task SaveAsync(string fileName, CancellationToken cancellationToken)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
            };

            foreach (ILibraryInstallationState library in _libraries)
            {
                if (library is LibraryInstallationState state)
                {
                    if (state.IsUsingDefaultDestination)
                    {
                        state.DestinationPath = null;
                    }
                    if (state.IsUsingDefaultProvider)
                    {
                        state.ProviderId = null;
                    }
                }
            }

            string json = JsonConvert.SerializeObject(this, settings);

            UpdateLibraryProviderAndDestination(this);

            byte[] buffer = Encoding.UTF8.GetBytes(json);

            using (FileStream writer = File.Create(fileName, 4096, FileOptions.Asynchronous))
            {
                await writer.WriteAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        ///  Deletes all library output files from disk.
        /// </summary>
        /// <remarks>
        /// The host calling this method provides the <paramref name="deleteFileAction"/>
        /// that deletes the files from the project.
        /// </remarks>
        /// <param name="deleteFileAction">>An action to delete the files.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ILibraryInstallationResult>> CleanAsync(Func<IEnumerable<string>, Task<bool>> deleteFileAction, CancellationToken cancellationToken)
        {
            var results = new List<ILibraryInstallationResult>();

            foreach (ILibraryInstallationState state in Libraries)
            {
                results.Add(await DeleteLibraryFilesAsync(state, deleteFileAction, cancellationToken));
            }

            return results;
        }

        /// <summary>
        /// Removes unwanted library files
        /// </summary>
        /// <param name="newManifest"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool> RemoveUnwantedFilesAsync(Manifest newManifest, CancellationToken cancellationToken)
        {
            if (newManifest != null)
            {
                ISet<FileIdentifier> existingFiles = await GetAllManifestFilesWithVersionsAsync(Libraries).ConfigureAwait(false);
                ISet<FileIdentifier> newFiles = await GetAllManifestFilesWithVersionsAsync(newManifest.Libraries).ConfigureAwait(false);
                IEnumerable<string> filesToRemove = existingFiles.Where(f => !newFiles.Contains(f)).Select(f => f.Path);

                if (filesToRemove.Any())
                {
                    IHostInteraction hostInteraction = _dependencies.GetHostInteractions();
                    return await hostInteraction.DeleteFilesAsync(filesToRemove, cancellationToken);
                }
            }

            return true;
        }

        private async Task<ISet<FileIdentifier>> GetAllManifestFilesWithVersionsAsync(IEnumerable<ILibraryInstallationState> libraries)
        {
            var files = new HashSet<FileIdentifier>();

            if (libraries == null)
            {
                return files;
            }

            foreach (ILibraryInstallationState state in libraries.Where(l => l.IsValid(out _)))
            {
                IProvider provider = _dependencies.GetProvider(state.ProviderId);

                if (provider == null)
                {
                    continue;
                }

                ILibraryInstallationResult updatedStateResult = await provider.UpdateStateAsync(state, CancellationToken.None);

                if (updatedStateResult.Success)
                {
                    IEnumerable<FileIdentifier> stateFiles = await GetFilesWithVersionsAsync(updatedStateResult.InstallationState).ConfigureAwait(false);

                    foreach (FileIdentifier fileIdentifier in stateFiles)
                    {
                        files.Add(fileIdentifier);
                    }
                }
            }

            return files;
        }

        private async Task<IEnumerable<FileIdentifier>> GetFilesWithVersionsAsync(ILibraryInstallationState state)
        {
            ILibraryCatalog catalog = _dependencies.GetProvider(state.ProviderId)?.GetCatalog();
            IEnumerable<FileIdentifier> filesWithVersions = new List<FileIdentifier>();

            if (catalog == null)
            {
                return filesWithVersions;
            }

            ILibrary library = await catalog.GetLibraryAsync(state.LibraryId, CancellationToken.None);

            if (library != null && library.Files != null)
            {
                IEnumerable<string> desiredStateFiles = state?.Files?.Where(f => library.Files.Keys.Contains(f));
                if (desiredStateFiles != null && desiredStateFiles.Any())
                {
                    filesWithVersions = desiredStateFiles.Select(f => new FileIdentifier(Path.Combine(state.DestinationPath, f), library.Version));
                }
            }

            return filesWithVersions;
        }

        private async Task<ILibraryInstallationResult> DeleteLibraryFilesAsync(ILibraryInstallationState state,
                                                       Func<IEnumerable<string>, Task<bool>> deleteFilesFunction,
                                                       CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return LibraryInstallationResult.FromCancelled(state);
            }

            try
            {
                IProvider provider = _dependencies.GetProvider(state.ProviderId);
                ILibraryInstallationResult updatedStateResult = await provider.UpdateStateAsync(state, CancellationToken.None);

                if (updatedStateResult.Success)
                {
                    List<string> filesToDelete = new List<string>();
                    state = updatedStateResult.InstallationState;

                    foreach (string file in state.Files)
                    {
                        var url = new Uri(file, UriKind.RelativeOrAbsolute);

                        if (!url.IsAbsoluteUri)
                        {
                            string relativePath = Path.Combine(state.DestinationPath, file).Replace('\\', '/');
                            filesToDelete.Add(relativePath);
                        }
                    }

                    bool success = true;
                    if (deleteFilesFunction != null)
                    {
                        success = await deleteFilesFunction.Invoke(filesToDelete);
                    }

                    if (success)
                    {
                        return LibraryInstallationResult.FromSuccess(updatedStateResult.InstallationState);
                    }
                    else
                    {
                        return LibraryInstallationResult.FromError(PredefinedErrors.CouldNotDeleteLibrary(state.LibraryId));
                    }
                }

                return updatedStateResult;
            }
            catch (Exception)
            {
                return LibraryInstallationResult.FromError(PredefinedErrors.CouldNotDeleteLibrary(state.LibraryId));
            }
        }
    }
}
