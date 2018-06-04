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
    internal class LibraryConflictDetector
    {
        public LibraryConflictDetector(IDependencies dependencies, string defaultDestination, string defaultProvider)
        {
            _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            _defaultDestination = defaultDestination;
            _defaultProvider = defaultProvider;
        }

        private string _defaultDestination;
        private string _defaultProvider;
        private IDependencies _dependencies;

        public async Task<IEnumerable<ILibraryInstallationResult>> DetectConflictsAsync(
            IEnumerable<ILibraryInstallationState> libraries,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IEnumerable<ILibraryInstallationResult> validateLibraries = ValidateLibrariesAsync(libraries, cancellationToken);

            if (!validateLibraries.All(t => t.Success))
            {
                return validateLibraries;
            }

            IEnumerable<ILibraryInstallationResult> expandLibraries= await ExpandLibrariesAsync(libraries, cancellationToken);
            if (!expandLibraries.All(t => t.Success))
            {
                return expandLibraries;
            }

            libraries = expandLibraries.Select(l => l.InstallationState);


            return new List<ILibraryInstallationResult> { GetConflictErrors(GetFilesConflicts(libraries, cancellationToken)) };

        }

        private IEnumerable<ILibraryInstallationResult> ValidateLibrariesAsync(IEnumerable<ILibraryInstallationState> libraries, CancellationToken cancellationToken)
        {
            List<ILibraryInstallationResult> validationStatus = new List<ILibraryInstallationResult>();

            foreach (ILibraryInstallationState library in libraries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!library.IsValid(out IEnumerable<IError> errors))
                {
                    validationStatus.Add(new LibraryInstallationResult(library, errors.ToArray()));
                }
                else
                {
                    validationStatus.Add(LibraryInstallationResult.FromSuccess(library));
                }
            }

            return validationStatus;
        }

        private async Task<IEnumerable<ILibraryInstallationResult>> ExpandLibrariesAsync(IEnumerable<ILibraryInstallationState> libraries, CancellationToken cancellationToken)
        {
            List<ILibraryInstallationResult> expandedLibraries = new List<ILibraryInstallationResult>();

            foreach (ILibraryInstallationState library in libraries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string installDestination = string.IsNullOrEmpty(library.DestinationPath) ? _defaultDestination : library.DestinationPath;
                string providerId = string.IsNullOrEmpty(library.ProviderId) ? _defaultProvider : library.ProviderId;
                IProvider provider = _dependencies.GetProvider(providerId);

                ILibraryInstallationResult desiredState = await provider.UpdateStateAsync(library, cancellationToken);

                expandedLibraries.Add(desiredState);
            }

            return expandedLibraries;
        }

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

        private ILibraryInstallationResult GetConflictErrors(IEnumerable<FileConflict> fileConflicts)
        {
            if (fileConflicts.Any())
            {
                var errors = new List<IError>();
                foreach (FileConflict conflictingLibraryGroup in fileConflicts)
                {
                    errors.Add(PredefinedErrors.ConflictingLibrariesInManifest(conflictingLibraryGroup.File, conflictingLibraryGroup.Libraries.Select(l => l.LibraryId).ToList()));
                }

                return new LibraryInstallationResult(errors.ToArray());
            }

            return LibraryInstallationResult.FromSuccess(null);
        }
    }


}
