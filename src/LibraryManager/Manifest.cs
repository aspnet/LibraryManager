// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.LibraryNaming;
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
        internal static Manifest FromJson(string json, IDependencies dependencies)
        {
            try
            {
                LibraryIdToNameAndVersionConverter.Instance.EnsureInitialized(dependencies);
                Manifest manifest = JsonConvert.DeserializeObject<Manifest>(json);
                manifest._dependencies = dependencies;
                manifest._hostInteraction = dependencies.GetHostInteractions();

                UpdateLibraryProviderAndDestination(manifest);

                return manifest;
            }
            catch (Exception)
            {
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
        public async Task<IEnumerable<ILibraryOperationResult>> InstallLibraryAsync(
            string libraryId,
            string providerId,
            IReadOnlyList<string> files,
            string destination,
            CancellationToken cancellationToken)
        {
            ILibraryOperationResult result;

            var desiredState = new LibraryInstallationState()
            {
                LibraryId = libraryId,
                Files = files,
                ProviderId = providerId,
                DestinationPath = destination
            };

            UpdateLibraryProviderAndDestination(desiredState, DefaultProvider, DefaultDestination);
            ILibraryOperationResult validationResult = await desiredState.IsValidAsync(_dependencies);
            if (!validationResult.Success)
            {
                return new [] { validationResult };
            }

            IProvider provider = _dependencies.GetProvider(desiredState.ProviderId);
            if (provider == null)
            {
                return new [] { new LibraryOperationResult(desiredState, new IError[] { PredefinedErrors.ProviderUnknown(desiredState.ProviderId) })};
            }

            IEnumerable<ILibraryOperationResult> conflictResults = await CheckLibraryForConflictsAsync(desiredState, cancellationToken).ConfigureAwait(false);

            if (!conflictResults.All(r => r.Success))
            {
                return conflictResults;
            }

            result = await provider.InstallAsync(desiredState, cancellationToken).ConfigureAwait(false);

            if (result.Success)
            {
                AddLibrary(desiredState);
            }

            return new [] { result };
        }

        private ILibraryInstallationState SetDefaultProviderIfNeeded(LibraryInstallationState desiredState)
        {
            if (string.IsNullOrEmpty(DefaultProvider))
            {
                DefaultProvider = desiredState.ProviderId;
                desiredState.IsUsingDefaultProvider = true;
            }
            else if (DefaultProvider.Equals(desiredState.ProviderId, StringComparison.OrdinalIgnoreCase))
            {
                desiredState.IsUsingDefaultProvider = true;
            }

            return desiredState;
        }

        private async Task<IEnumerable<ILibraryOperationResult>> CheckLibraryForConflictsAsync(ILibraryInstallationState desiredState, CancellationToken cancellationToken)
        {
            var libraries = new List<ILibraryInstallationState>(Libraries);
            libraries.Add(desiredState);

            IEnumerable<ILibraryOperationResult> fileConflicts = await LibrariesValidator.GetLibrariesErrorsAsync(libraries, _dependencies, DefaultDestination, DefaultProvider, cancellationToken).ConfigureAwait(false);

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
            {
                _libraries.Remove(existing);
            }

            if (state is LibraryInstallationState desiredState)
            {
                _libraries.Add(SetDefaultProviderIfNeeded(desiredState));
            }
            else
            {
                _libraries.Add(state);
            }
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
        public async Task<IEnumerable<ILibraryOperationResult>> RestoreAsync(CancellationToken cancellationToken)
        {
            //TODO: This should have an "undo scope"
            var results = new List<ILibraryOperationResult>();

            foreach (ILibraryInstallationState state in Libraries)
            {
                results.Add(await RestoreLibraryAsync(state, cancellationToken).ConfigureAwait(false));
            }

            return results;
        }

        /// <summary>
        /// Returns a collection of <see cref="ILibraryOperationResult"/> that represents the status for validation of the Manifest and its libraries
        /// </summary>
        /// <param name="cancellationToken">A token that allows for cancellation of the operation.</param>
        public async Task<IEnumerable<ILibraryOperationResult>> GetValidationResultsAsync(CancellationToken cancellationToken)
        {
            IEnumerable<ILibraryOperationResult> validationResults = await LibrariesValidator.GetManifestErrorsAsync(this, _dependencies, cancellationToken).ConfigureAwait(false);

            return validationResults;
        }

        private async Task<ILibraryOperationResult> RestoreLibraryAsync(ILibraryInstallationState libraryState, CancellationToken cancellationToken)
        {
            _hostInteraction.Logger.Log(string.Format(Resources.Text.Restore_RestoreOfLibraryStarted, libraryState.LibraryId, libraryState.DestinationPath), LogLevel.Operation);

            if (cancellationToken.IsCancellationRequested)
            {
                return LibraryOperationResult.FromCancelled(libraryState);
            }

            IProvider provider = _dependencies.GetProvider(libraryState.ProviderId);
            if (provider == null)
            {
                return new LibraryOperationResult(libraryState, new IError[] { PredefinedErrors.ProviderUnknown(libraryState.ProviderId) });
            }

            try
            {
                return await provider.InstallAsync(libraryState, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return LibraryOperationResult.FromCancelled(libraryState);
            }
        }

        /// <summary>
        /// Uninstalls the specified library and removes it from the <see cref="Libraries"/> collection.
        /// </summary>
        /// <param name="libraryId">The library identifier.</param>
        /// <param name="deleteFilesFunction"></param>
        /// <param name="cancellationToken"></param>
        public async Task<ILibraryOperationResult> UninstallAsync(string libraryId, Func<IEnumerable<string>, Task<bool>> deleteFilesFunction, CancellationToken cancellationToken)
        {
            ILibraryInstallationState library = Libraries.FirstOrDefault(l => l.LibraryId == libraryId);

            if (cancellationToken.IsCancellationRequested)
            {
                return LibraryOperationResult.FromCancelled(library);
            }

            if (library != null)
            {
                ILibraryOperationResult validationResult = await library.IsValidAsync(_dependencies);
                if (!validationResult.Success)
                {
                    return validationResult;
                }

                try
                {
                    ILibraryOperationResult result = await DeleteLibraryFilesAsync(library, deleteFilesFunction, cancellationToken).ConfigureAwait(false);

                    if (result.Success)
                    {
                        _libraries.Remove(library);

                        return result;
                    }
                }
                catch (OperationCanceledException)
                {
                    return LibraryOperationResult.FromCancelled(library);
                }
            }

            return LibraryOperationResult.FromError(PredefinedErrors.CouldNotDeleteLibrary(library.LibraryId));
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
        public async Task<IEnumerable<ILibraryOperationResult>> CleanAsync(Func<IEnumerable<string>, Task<bool>> deleteFileAction, CancellationToken cancellationToken)
        {
            var results = new List<ILibraryOperationResult>();

            foreach (ILibraryInstallationState state in Libraries)
            {
                results.Add(await DeleteLibraryFilesAsync(state, deleteFileAction, cancellationToken).ConfigureAwait(false));
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
            cancellationToken.ThrowIfCancellationRequested();

            if (newManifest != null)
            {
                IEnumerable<FileIdentifier> existingFiles = await GetAllManifestFilesWithVersionsAsync(Libraries).ConfigureAwait(false);
                IEnumerable<FileIdentifier> newFiles = await GetAllManifestFilesWithVersionsAsync(newManifest.Libraries).ConfigureAwait(false);
                IEnumerable<string> filesToRemove = existingFiles.Except(newFiles).Select(f => f.Path);

                if (filesToRemove.Any())
                {
                    IHostInteraction hostInteraction = _dependencies.GetHostInteractions();
                    return await hostInteraction.DeleteFilesAsync(filesToRemove, cancellationToken).ConfigureAwait(false);
                }
            }

            return true;
        }

        private async Task<IEnumerable<FileIdentifier>> GetAllManifestFilesWithVersionsAsync(IEnumerable<ILibraryInstallationState> libraries)
        {
            var tasks = new List<Task<IEnumerable<FileIdentifier>>>();

            if (libraries != null)
            {
                foreach (ILibraryInstallationState state in libraries)
                {
                    tasks.Add(GetFilesWithVersionsAsync(state));
                }

                IEnumerable<FileIdentifier>[] allFiles = await Task.WhenAll(tasks).ConfigureAwait(false);

                return allFiles.SelectMany(f => f).Distinct();
            }

           return new List<FileIdentifier>();
        }

        private async Task<IEnumerable<FileIdentifier>> GetFilesWithVersionsAsync(ILibraryInstallationState state)
        {
            IEnumerable<FileIdentifier> filesWithVersions = new List<FileIdentifier>();
            ILibraryCatalog catalog = _dependencies.GetProvider(state.ProviderId)?.GetCatalog();

            if (catalog == null)
            {
                return filesWithVersions;
            }

            ILibraryOperationResult validationResult = await state.IsValidAsync(_dependencies).ConfigureAwait(false);
            if (validationResult.Success)
            {
                IProvider provider = _dependencies.GetProvider(state.ProviderId);

                if (provider != null)
                {
                    ILibraryOperationResult updatedStateResult = await provider.UpdateStateAsync(state, CancellationToken.None).ConfigureAwait(false);

                    if (updatedStateResult.Success)
                    {
                        ILibrary library = await catalog.GetLibraryAsync(state.LibraryId, CancellationToken.None).ConfigureAwait(false);

                        if (library != null && library.Files != null)
                        {
                            IEnumerable<string> desiredStateFiles = updatedStateResult.InstallationState.Files;
                            if (desiredStateFiles != null && desiredStateFiles.Any())
                            {
                                filesWithVersions = desiredStateFiles.Select(f => new FileIdentifier(Path.Combine(state.DestinationPath, f), library.Version));
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.Assert(validationResult.Success);
            }

            return filesWithVersions;
        }

        private async Task<ILibraryOperationResult> DeleteLibraryFilesAsync(ILibraryInstallationState state,
                                                       Func<IEnumerable<string>, Task<bool>> deleteFilesFunction,
                                                       CancellationToken cancellationToken)
        {

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                IProvider provider = _dependencies.GetProvider(state.ProviderId);
                ILibraryOperationResult updatedStateResult = await provider.UpdateStateAsync(state, CancellationToken.None).ConfigureAwait(false);

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
                        success = await deleteFilesFunction.Invoke(filesToDelete).ConfigureAwait(false);
                    }

                    if (success)
                    {
                        return LibraryOperationResult.FromSuccess(updatedStateResult.InstallationState);
                    }
                    else
                    {
                        return LibraryOperationResult.FromError(PredefinedErrors.CouldNotDeleteLibrary(state.LibraryId));
                    }
                }

                return updatedStateResult;
            }
            catch (OperationCanceledException)
            {
                return LibraryOperationResult.FromCancelled(state);
            }
            catch (Exception)
            {
                return LibraryOperationResult.FromError(PredefinedErrors.CouldNotDeleteLibrary(state.LibraryId));
            }
        }
    }
}
