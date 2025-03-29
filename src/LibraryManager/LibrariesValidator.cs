// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Helpers;

namespace Microsoft.Web.LibraryManager
{
    /// <summary>
    /// Finds conflicts between different libraries, based on files brought in by each library.
    /// </summary>
    internal static class LibrariesValidator
    {
        /// <summary>
        /// Returns a collection of <see cref="OperationResult{LibraryInstallationGoalState}"/> that represents the status for validation of each
        ///  library
        /// </summary>
        /// <param name="libraries">Set of libraries to be validated</param>
        /// <param name="dependencies"><see cref="IDependencies"/>used to validate the libraries</param>
        /// <param name="defaultDestination">Default destination used to validate the libraries</param>
        /// <param name="defaultProvider">DefaultProvider used to validate the libraries</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<OperationResult<LibraryInstallationGoalState>>> GetLibrariesErrorsAsync(
            IEnumerable<ILibraryInstallationState> libraries,
            IDependencies dependencies,
            string defaultDestination,
            string defaultProvider,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Check for valid libraries
            IEnumerable<OperationResult<LibraryInstallationGoalState>> validateLibraries = await ValidatePropertiesAsync(libraries, dependencies, cancellationToken).ConfigureAwait(false);
            if (!validateLibraries.All(t => t.Success))
            {
                return validateLibraries;
            }

            // Check for duplicate libraries
            IEnumerable<OperationResult<LibraryInstallationGoalState>> duplicateLibraries = GetDuplicateLibrariesErrors(libraries);
            if (!duplicateLibraries.All(t => t.Success))
            {
                return duplicateLibraries;
            }

            // Check for files conflicts
            IEnumerable<OperationResult<LibraryInstallationGoalState>> result = await ExpandLibrariesAsync(libraries, dependencies, defaultDestination, defaultProvider, cancellationToken).ConfigureAwait(false);
            if (!result.All(t => t.Success))
            {
                return result;
            }

            IEnumerable<LibraryInstallationGoalState> goalStates = result.Select(l => l.Result);
            IEnumerable<FileConflict> fileConflicts = GetFilesConflicts(goalStates);
            if (fileConflicts.Any())
            {
                result = [GetConflictErrors(fileConflicts)];
            }

            return result;
        }

        /// <summary>
        /// Returns a collection of <see cref="OperationResult{LibraryInstallationGoalState}"/> that represents the status for validation of the Manifest and its libraries
        /// </summary>
        /// <param name="manifest">The <see cref="Manifest"/> to be validated</param>
        /// <param name="dependencies"><see cref="IDependencies"/>used to validate the libraries</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<OperationResult<LibraryInstallationGoalState>>> GetManifestErrorsAsync(
            Manifest manifest,
            IDependencies dependencies,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (manifest == null)
            {
                return [OperationResult<LibraryInstallationGoalState>.FromError(PredefinedErrors.ManifestMalformed())];
            }

            if (string.IsNullOrEmpty(manifest.Version))
            {
                return [OperationResult<LibraryInstallationGoalState>.FromError(PredefinedErrors.MissingManifestVersion())];
            }

            if (!IsValidManifestVersion(manifest.Version))
            {
                return [OperationResult<LibraryInstallationGoalState>.FromError(PredefinedErrors.VersionIsNotSupported(manifest.Version))];
            }

            return await GetLibrariesErrorsAsync(manifest.Libraries, dependencies, manifest.DefaultDestination, manifest.DefaultProvider, cancellationToken);
        }

        private static bool IsValidManifestVersion(string version)
        {
            Version parsedVersion;
            if (Version.TryParse(version, out parsedVersion))
            {
                return Manifest.SupportedVersions.Contains(parsedVersion);
            }

            return false;
        }

        /// <summary>
        /// Validates the values of each Library property and returns a collection of ILibraryOperationResult for each of them
        /// </summary>
        /// <param name="libraries"></param>
        /// <param name="dependencies"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task<IEnumerable<OperationResult<LibraryInstallationGoalState>>> ValidatePropertiesAsync(IEnumerable<ILibraryInstallationState> libraries, IDependencies dependencies, CancellationToken cancellationToken)
        {
            var validationStatus = new List<OperationResult<LibraryInstallationGoalState>>();

            foreach (ILibraryInstallationState library in libraries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                OperationResult<LibraryInstallationGoalState> result = await library.IsValidAsync(dependencies).ConfigureAwait(false);
                validationStatus.Add(result);
            }

            return validationStatus;
        }

        private static IEnumerable<OperationResult<LibraryInstallationGoalState>> GetDuplicateLibrariesErrors(IEnumerable<ILibraryInstallationState> libraries)
        {
            var errors = new List<OperationResult<LibraryInstallationGoalState>>();
            HashSet<string> duplicateLibraries = GetDuplicateLibraries(libraries);

            if (duplicateLibraries.Count > 0)
            {
                foreach (string libraryName in duplicateLibraries)
                {
                    errors.Add(OperationResult<LibraryInstallationGoalState>.FromError(PredefinedErrors.DuplicateLibrariesInManifest(libraryName)));
                }
            }

            return errors;
        }

        private static HashSet<string> GetDuplicateLibraries(IEnumerable<ILibraryInstallationState> libraries)
        {
            var librariesNames = new HashSet<string>();
            var duplicateNames = new HashSet<string>();
            foreach (ILibraryInstallationState library in libraries)
            {
                if (!librariesNames.Add(library.Name))
                {
                    duplicateNames.Add(library.Name);
                }
            }

            return duplicateNames;
        }

        /// <summary>
        /// Expands the files property for each library
        /// </summary>
        /// <param name="libraries"></param>
        /// <param name="dependencies"></param>
        /// <param name="defaultDestination"></param>
        /// <param name="defaultProvider"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task<IEnumerable<OperationResult<LibraryInstallationGoalState>>> ExpandLibrariesAsync(
            IEnumerable<ILibraryInstallationState> libraries,
            IDependencies dependencies,
            string defaultDestination,
            string defaultProvider,
            CancellationToken cancellationToken)
        {
            List<OperationResult<LibraryInstallationGoalState>> expandedLibraries = [];

            foreach (ILibraryInstallationState library in libraries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string installDestination = string.IsNullOrEmpty(library.DestinationPath) ? defaultDestination : library.DestinationPath;
                string providerId = string.IsNullOrEmpty(library.ProviderId) ? defaultProvider : library.ProviderId;

                IProvider provider = dependencies.GetProvider(providerId);
                if (provider == null)
                {
                    return [OperationResult<LibraryInstallationGoalState>.FromError(PredefinedErrors.ProviderIsUndefined())];
                }

                OperationResult<LibraryInstallationGoalState> desiredState = await provider.GetInstallationGoalStateAsync(library, cancellationToken);
                if (!desiredState.Success)
                {
                    return [desiredState];
                }

                expandedLibraries.Add(desiredState);
            }

            return expandedLibraries;
        }

        /// <summary>
        /// Detects files conflicts in between libraries in the given collection
        /// </summary>
        /// <param name="goalStates"></param>
        /// <returns>A collection of <see cref="FileConflict"/> for each library conflict</returns>
        private static IEnumerable<FileConflict> GetFilesConflicts(IEnumerable<LibraryInstallationGoalState> goalStates)
        {
            Dictionary<string, List<LibraryInstallationGoalState>> fileToLibraryMap = new(RelativePathEqualityComparer.Instance);

            foreach (LibraryInstallationGoalState goalState in goalStates)
            {
                IEnumerable<string> files = goalState.InstalledFiles.Keys;

                foreach (string file in files)
                {
                    if (!fileToLibraryMap.ContainsKey(file))
                    {
                        fileToLibraryMap[file] = new List<LibraryInstallationGoalState>();
                    }

                    fileToLibraryMap[file].Add(goalState);
                }
            }

            return fileToLibraryMap
                    .Where(f => f.Value.Count > 1)
                    .Select(f => new FileConflict(f.Key, f.Value.Select(gs => gs.InstallationState).ToList()));

        }

        /// <summary>
        /// Generates a single ILibraryOperationResult with a collection of IErros based on the collection of FileConflict
        /// </summary>
        /// <param name="fileConflicts"></param>
        /// <returns></returns>
        private static OperationResult<LibraryInstallationGoalState> GetConflictErrors(IEnumerable<FileConflict> fileConflicts)
        {
            if (fileConflicts.Any())
            {
                var errors = new List<IError>();
                foreach (FileConflict conflictingLibraryGroup in fileConflicts)
                {
                    errors.Add(PredefinedErrors.ConflictingFilesInManifest(conflictingLibraryGroup.File, conflictingLibraryGroup.Libraries.Select(l => l.Name).ToList()));
                }

                return OperationResult<LibraryInstallationGoalState>.FromErrors([..errors]);
            }

            return OperationResult<LibraryInstallationGoalState>.FromSuccess(null);
        }
    }
}
