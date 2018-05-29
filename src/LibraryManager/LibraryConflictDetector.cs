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
    /// Finds conflicts between different libraries, based on clashing files.
    /// </summary>
    internal class LibraryConflictDetector
    {
        public LibraryConflictDetector(IDependencies dependencies, string defaultDestination, string defaultProvider)
        {
            _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            _defaultDestination = defaultDestination;
            _defaultProvider = defaultProvider;
            _fileToLibraryMap = new Dictionary<string, List<ILibraryInstallationState>>(PathEqualityComparer.Instance);
        }

        private string _defaultDestination;
        private string _defaultProvider;
        private IDependencies _dependencies;
        private Dictionary<string, List<ILibraryInstallationState>> _fileToLibraryMap;

        public ISet<ILibraryInstallationState> ConflictingLibraries { get; private set; }

        public IEnumerable<KeyValuePair<string, List<ILibraryInstallationState>>> FileConflicts =>
            _fileToLibraryMap.Where(f => f.Value.Count > 1);


        /// <summary>
        /// Finds all Libraries, that have conflicting files.
        /// </summary>
        /// <param name="libraries"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ISet<ILibraryInstallationState>> FindAllConflictingLibrariesAsync(IEnumerable<ILibraryInstallationState> libraries,
            CancellationToken cancellationToken)
        {
            await DetectConflictsAsync(libraries, cancellationToken);

            return ConflictingLibraries;
        }


        public async Task<IEnumerable<FileBasedConflict>> DetectConflictsAsync(
            IEnumerable<ILibraryInstallationState> libraries,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _fileToLibraryMap.Clear();
            ConflictingLibraries = new HashSet<ILibraryInstallationState>();

            foreach (ILibraryInstallationState state in libraries)
            {
                if (!state.IsValid(out IEnumerable<IError> _))
                {
                    continue;
                }

                string installDestination = string.IsNullOrEmpty(state.DestinationPath) ? _defaultDestination : state.DestinationPath;
                string providerId = string.IsNullOrEmpty(state.ProviderId) ? _defaultProvider : state.ProviderId;
                IProvider provider = _dependencies.GetProvider(providerId);

                if (provider == null)
                {
                    continue;
                }

                ILibraryInstallationResult desiredState = await provider.UpdateStateAsync(state, cancellationToken);
                if (desiredState.Success)
                {
                    IEnumerable<string> files = desiredState.InstallationState.Files.Select(f => Path.Combine(installDestination, f));

                    foreach (string file in files)
                    {
                        if (!_fileToLibraryMap.ContainsKey(file))
                        {
                            _fileToLibraryMap[file] = new List<ILibraryInstallationState>();
                        }
                        else
                        {
                            ConflictingLibraries.Add(state);
                        }

                        _fileToLibraryMap[file].Add(state);
                    }
                }
            }

            return _fileToLibraryMap.Where(f => f.Value.Count > 1).Select(f => new FileBasedConflict(f.Key, f.Value));
        }
    }

    internal class FileBasedConflict
    {

        public FileBasedConflict(string file, List<ILibraryInstallationState> libraries)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentException(nameof(file));
            }

            File = file;
            Libraries = libraries ?? throw new ArgumentNullException(nameof(libraries));
        }

        public string File { get; }
        public IList<ILibraryInstallationState> Libraries { get; }
    }
}
