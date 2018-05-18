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
                string json = await FileHelpers.ReadFileTextAsync(fileName, cancellationToken).ConfigureAwait(false);
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

                foreach (LibraryInstallationState state in manifest.Libraries.Cast<LibraryInstallationState>())
                {
                    state.ProviderId = state.ProviderId ?? manifest.DefaultProvider;
                    state.DestinationPath = state.DestinationPath ?? manifest.DefaultDestination;
                }

                return manifest;
            }
            catch (Exception)
            {
                dependencies.GetHostInteractions().Logger.Log(PredefinedErrors.ManifestMalformed().Message, LogLevel.Task);
                return null;
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
            var results = new List<ILibraryInstallationResult>();

            if (!IsValidManifestVersion(Version))
            {
                return new ILibraryInstallationResult[] { LibraryInstallationResult.FromError(PredefinedErrors.VersionIsNotSupported(Version)) };
            }

            foreach (ILibraryInstallationState state in Libraries)
            {
                tasks.Add(RestoreLibraryAsync(state, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return tasks.Select(t => t.Result);
        }

        private async Task<ILibraryInstallationResult> RestoreLibraryAsync(ILibraryInstallationState libraryState, CancellationToken cancellationToken)
        {
            _hostInteraction.Logger.Log(string.Format(Resources.Text.RestoringLibrary, libraryState.LibraryId), LogLevel.Operation);

            if (cancellationToken.IsCancellationRequested)
            {
                return LibraryInstallationResult.FromCancelled(libraryState);
            }

            if (!libraryState.IsValid(out IEnumerable<IError> errors))
            {
                return new LibraryInstallationResult(libraryState, errors.ToArray());
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
        /// <param name="deleteFileFunction"></param>
        /// <param name="cancellationToken"></param>
        public async Task<ILibraryInstallationResult> UninstallAsync(string libraryId, Func<IEnumerable<string>, Task<bool>> deleteFileFunction, CancellationToken cancellationToken)
        {
            ILibraryInstallationState state = Libraries.FirstOrDefault(l => l.LibraryId == libraryId);

            if (cancellationToken.IsCancellationRequested)
            {
                return LibraryInstallationResult.FromCancelled(state);
            }

            ILibraryInstallationResult result = LibraryInstallationResult.FromError(PredefinedErrors.CouldNotDeleteLibrary(state.LibraryId));

            if (state != null)
            {
                result = await DeleteLibraryFilesAsync(state, deleteFileFunction, cancellationToken);

                if (result.Success)
                {
                    _libraries.Remove(state);
                }
            }

            return result;
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

            string json = JsonConvert.SerializeObject(this, settings);
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
        public async Task<IEnumerable<ILibraryInstallationResult>> CleanAsync(Func<IEnumerable<string>, Task<bool>> deleteFileAction,
                                                                              CancellationToken cancellationToken)
        {
            List<Task<ILibraryInstallationResult>> cleanTasks = new List<Task<ILibraryInstallationResult>>();

            foreach (ILibraryInstallationState state in Libraries)
            {
                cleanTasks.Add(DeleteLibraryFilesAsync(state, deleteFileAction, cancellationToken));
            }

            await Task.WhenAll(cleanTasks).ConfigureAwait(false);

            return cleanTasks.Select(t => t.Result);
        }

        private async Task<ILibraryInstallationResult> DeleteLibraryFilesAsync(ILibraryInstallationState state,
                                                       Func<IEnumerable<string>, Task<bool>> deleteFilesFunction,
                                                       CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return LibraryInstallationResult.FromCancelled(state);
            }

            IProvider provider = _dependencies.GetProvider(state.ProviderId);
            ILibraryInstallationResult updatedStateResult = provider.UpdateStateAsync(state, CancellationToken.None).Result;

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

                bool success = await deleteFilesFunction?.Invoke(filesToDelete);

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
    }
}
