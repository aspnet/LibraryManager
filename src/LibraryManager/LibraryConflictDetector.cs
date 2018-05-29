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
            _fileToLibraryMap = new Dictionary<string, List<ILibraryInstallationState>>(RelativePathEqualityComparer.Instance);
        }

        private string _defaultDestination;
        private string _defaultProvider;
        private IDependencies _dependencies;
        private Dictionary<string, List<ILibraryInstallationState>> _fileToLibraryMap;

        public async Task<IEnumerable<FileConflict>> DetectConflictsAsync(
            IEnumerable<ILibraryInstallationState> libraries,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _fileToLibraryMap.Clear();

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

                        _fileToLibraryMap[file].Add(state);
                    }
                }
            }

            return _fileToLibraryMap
                .Where(f => f.Value.Count > 1)
                .Select(f => new FileConflict(f.Key, f.Value));
        }
    }


}
