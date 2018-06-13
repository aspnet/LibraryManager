// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager
{
    /// <summary>
    /// Finds conflicts between different libraries, based on files brought in by each library.
    /// </summary>
    internal class LibrariesValidator
    {
        private string _defaultDestination;
        private string _defaultProvider;
        private IDependencies _dependencies;

        /// <summary>
        /// Validates set of libraries given the dependencies, default destination and default provider
        /// </summary>
        /// <param name="dependencies"></param>
        /// <param name="defaultDestination"></param>
        /// <param name="defaultProvider"></param>
        public LibrariesValidator(IDependencies dependencies, string defaultDestination, string defaultProvider)
        {
            _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            _defaultDestination = defaultDestination;
            _defaultProvider = defaultProvider;
        }
        
        /// <summary>
        /// Validates all properties of the library including detection of invalid files
        /// </summary>
        /// <param name="library"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ILibraryOperationResult> ValidateLibraryAsync(ILibraryInstallationState library, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var list = new List<IError>();

            // Validates provider
            IError error;
            IProvider provider = GetProvider(library, out error);
            if (provider == null)
            {
                list.Add(error);
            }
            else
            {
                // Validates libraryId
                if (!IsValidLibraryId(library, provider, out error))
                {
                    list.Add(error);
                }
            }

            // Validates destinationPath
            if (!IsValidDestinationPath(library, out error))
            {
                list.Add(error);
            }

            // Validates list of files if specified
            ILibraryCatalog catalog = provider.GetCatalog();
            if (catalog != null)
            {
                ILibraryInstallationState stateToValidate = library;
                if (library.Files != null)
                {
                    ILibraryOperationResult result = await ValidateLibraryFilesAsync(stateToValidate, catalog, cancellationToken);
                    if (!result.Success)
                    {
                        list.AddRange(result.Errors);
                    }
                }
            }

            if (list.Any())
            {
                return new LibraryOperationResult(list.ToArray());
            }

            return LibraryOperationResult.FromSuccess(library);
        }

        private bool IsValidDestinationPath(ILibraryInstallationState library, out IError error)
        {
            if (string.IsNullOrEmpty(library.DestinationPath) && string.IsNullOrEmpty(_defaultDestination))
            {
                error = PredefinedErrors.PathIsUndefined();

                return false;
            }
            else
            {
                string destinationPath = library.DestinationPath ?? _defaultDestination;

                if (destinationPath.IndexOfAny(Path.GetInvalidPathChars()) > 0)
                {
                    error = PredefinedErrors.DestinationPathHasInvalidCharacters(destinationPath);

                    return false;
                }
            }

            error = null;

            return true;
        }

        private bool IsValidLibraryId(ILibraryInstallationState library, IProvider provider, out IError error)
        {
            if (string.IsNullOrEmpty(library.LibraryId))
            {
                error = PredefinedErrors.LibraryIdIsUndefined();
                return false;
            }
            else
            {
                try
                {
                    provider.GetLibraryIdentifier(library.LibraryId);
                }
                catch (InvalidLibraryException)
                {
                    error = PredefinedErrors.UnableToResolveSource(library.LibraryId, provider.Id);
                    return false;
                }
            }

            error = null;
            return true;
        }

        private IProvider GetProvider(ILibraryInstallationState library, out IError error)
        {
            if (string.IsNullOrEmpty(library.ProviderId) && string.IsNullOrEmpty(_defaultProvider))
            {
                error =  PredefinedErrors.ProviderIsUndefined();
                return null;
            }

            string providerId = library.ProviderId ?? _defaultProvider;
            IProvider provider = _dependencies.GetProvider(providerId);
            if (provider == null)
            {
                error = PredefinedErrors.ProviderUnknown(library.ProviderId);
                return null;
            }

            error = null;
            return provider;
        }

        private async Task<ILibraryOperationResult> ValidateLibraryFilesAsync(ILibraryInstallationState desiredLibraryState, ILibraryCatalog catalog, CancellationToken cancellationToken)
        {
            ILibrary library = await catalog.GetLibraryAsync(desiredLibraryState.LibraryId, CancellationToken.None).ConfigureAwait(false);

            if (library != null && library.Files != null)
            {
                var desiredLibraryFiles = new HashSet<string>(desiredLibraryState.Files);
                var actualStateFiles = new HashSet<string>(library.Files.Keys);
                IEnumerable<string> invalidFiles = desiredLibraryFiles.Except(actualStateFiles);

                if (invalidFiles.Any())
                {
                    return LibraryOperationResult.FromError(PredefinedErrors.InvalidFilesInLibrary(desiredLibraryState.LibraryId, invalidFiles, actualStateFiles));
                }
            }

            return LibraryOperationResult.FromSuccess(desiredLibraryState);
        }

        /// <summary>
        /// Returns a collection of ILibraryOperationResult that represents the status for validation of each 
        ///  library 
        /// </summary>
        /// <param name="libraries"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ILibraryOperationResult>> ValidateLibrariesAsync(
            IEnumerable<ILibraryInstallationState> libraries,
            CancellationToken cancellationToken)
        {

            cancellationToken.ThrowIfCancellationRequested();

            // Check for duplicate libraries 
            IEnumerable<string> duplicates = GetDuplicateLibraries(libraries, cancellationToken);
            if (duplicates.Any())
            {
                return new[] { LibraryOperationResult.FromError(PredefinedErrors.DuplicateLibrariesInManifest(duplicates)) };
            }

            // Check for invalid libraries
            List<Task<ILibraryOperationResult>> validationTasks = new List<Task<ILibraryOperationResult>>();
            foreach (ILibraryInstallationState library in libraries)
            {
                validationTasks.Add(ValidateLibraryAsync(library, cancellationToken));
            }

            await Task.WhenAll(validationTasks);

            IEnumerable<ILibraryOperationResult> validationResults = validationTasks.Select(t => t.Result);
            ILibraryOperationResult failedValidation = validationResults.Where(r => !r.Success).FirstOrDefault();

            if (failedValidation != null)
            {
                return new[] { failedValidation };
            }

            // Check for file duplication
            return new List<ILibraryOperationResult> { GetConflictErrors(GetFilesConflicts(libraries, cancellationToken)) };

        }

        private IEnumerable<string> GetDuplicateLibraries(IEnumerable<ILibraryInstallationState> libraries, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<string> duplicateLibraries = new List<string>();
            IEnumerable<IProvider> providers = GetProviders(libraries);

            foreach (IProvider provider in providers)
            {
                IEnumerable<ILibraryInstallationState> providerLibraries = libraries.Where(l => l.ProviderId == provider.Id);
                duplicateLibraries.AddRange(providerLibraries.GroupBy(l => GetLibraryName(provider, l.LibraryId, cancellationToken)).Where(g => g.Count() > 1).Select(g => g.Key));
            }

            return duplicateLibraries;
        }

        private string GetLibraryName(IProvider provider, string libraryId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                LibraryIdentifier libraryIdentifier = provider.GetLibraryIdentifier(libraryId);

                return libraryIdentifier.Name;
            }
            catch (InvalidLibraryException)
            {
                return null;
            }
        }

        private IEnumerable<IProvider> GetProviders(IEnumerable<ILibraryInstallationState> libraries)
        {
            var providers = new List<IProvider>();

            foreach (ILibraryInstallationState library in libraries)
            {

                IProvider provider = GetProvider(library, out _);
                if (provider != null)
                {
                    providers.Add(provider);
                }
            }

            return providers;
        }

        
        /// <summary>
        /// Detects files conflicts in between libraries in the given collection 
        /// </summary>
        /// <param name="libraries"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>A colletion of FileConflict for each conflict</returns>
        public IEnumerable<FileConflict> GetFilesConflicts(IEnumerable<ILibraryInstallationState> libraries, CancellationToken cancellationToken)
        {
            Dictionary<string, List<ILibraryInstallationState>> _fileToLibraryMap = new Dictionary<string, List<ILibraryInstallationState>>(RelativePathEqualityComparer.Instance);

            foreach (ILibraryInstallationState library in libraries)
            {
                string destinationPath = library.DestinationPath;

                IEnumerable<string> files = library.Files.Select(f => Path.Combine(destinationPath, f));

                foreach (string file in files)
                {
                    if (!_fileToLibraryMap.ContainsKey(file))
                    {
                        _fileToLibraryMap[file] = new List<ILibraryInstallationState>();
                    }

                    _fileToLibraryMap[file].Add(library);
                }
            }

            return _fileToLibraryMap
                    .Where(f => f.Value.Count > 1)
                    .Select(f => new FileConflict(f.Key, f.Value));

        }

        /// <summary>
        /// Generates a single ILibraryOperationResult with a collection of IErros based on the collection of FileConflict
        /// </summary>
        /// <param name="fileConflicts"></param>
        /// <returns></returns>
        public ILibraryOperationResult GetConflictErrors(IEnumerable<FileConflict> fileConflicts)
        {
            if (fileConflicts.Any())
            {
                var errors = new List<IError>();
                foreach (FileConflict conflictingLibraryGroup in fileConflicts)
                {
                    errors.Add(PredefinedErrors.ConflictingLibrariesInManifest(conflictingLibraryGroup.File, conflictingLibraryGroup.Libraries.Select(l => l.LibraryId).ToList()));
                }

                return new LibraryOperationResult(errors.ToArray());
            }

            return LibraryOperationResult.FromSuccess(null);
        }
    }


}
