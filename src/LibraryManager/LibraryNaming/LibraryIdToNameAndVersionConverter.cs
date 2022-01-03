// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.LibraryNaming
{
    /// <summary>
    /// Defines the way LibraryId is split into name and Version
    /// </summary>
    public class LibraryIdToNameAndVersionConverter
    {
        /// <summary>
        /// Singleton instance for the <see cref="LibraryIdToNameAndVersionConverter" />
        /// </summary>
        public static LibraryIdToNameAndVersionConverter Instance { get; } = new LibraryIdToNameAndVersionConverter();

        private LibraryIdToNameAndVersionConverter()
        {
            _versionedNamingScheme = new VersionedLibraryNamingScheme();
            _simpleNamingScheme = new SimpleLibraryNamingScheme();
            _default = _simpleNamingScheme;
            _isInitialized = false;

            _perProviderNamingScheme = new Dictionary<string, ILibraryNamingScheme>(StringComparer.OrdinalIgnoreCase);
        }

        private readonly object _syncObject = new object();
        private readonly ILibraryNamingScheme _versionedNamingScheme;
        private readonly ILibraryNamingScheme _simpleNamingScheme;
        private readonly ILibraryNamingScheme _default;


        private IDependencies _dependencies;
        private Dictionary<string, ILibraryNamingScheme> _perProviderNamingScheme;
        private bool _isInitialized;

        /// <summary>
        /// Initializes the LibraryNaming Schemes for the manifest.
        /// </summary>
        /// <param name="dependencies"></param>
        public void EnsureInitialized (IDependencies dependencies)
        {
            if (_isInitialized)
            {
                return;
            }

            lock(_syncObject)
            {
                if (_isInitialized)
                {
                    return;
                }

                _isInitialized = true;
                _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));

                _perProviderNamingScheme.Clear();

                foreach(IProvider p in _dependencies.Providers)
                {
                    _perProviderNamingScheme[p.Id] = p.SupportsLibraryVersions ? _versionedNamingScheme : _simpleNamingScheme;
                }
            }
        }

        /// <summary>
        /// TEST ONLY: re-initialize in order to accommodate changes to the IDependencies per test
        /// </summary>
        /// <param name="dependencies"></param>
        internal void Reinitialize(IDependencies dependencies)
        {
            lock (_syncObject)
            {
                _isInitialized = true;
                _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));

                _perProviderNamingScheme.Clear();

                foreach (IProvider p in _dependencies.Providers)
                {
                    _perProviderNamingScheme[p.Id] = p.SupportsLibraryVersions ? _versionedNamingScheme : _simpleNamingScheme;
                }
            }
        }

        /// <summary>
        /// Splits the libraryId into name and version based on the provider.
        /// </summary>
        /// <param name="libraryId"></param>
        /// <param name="providerId"></param>
        /// <returns></returns>
        public (string Name, string Version) GetLibraryNameAndVersion(string libraryId, string providerId)
        {
            return GetSchemeForProvider(providerId).GetLibraryNameAndVersion(libraryId);
        }

        /// <summary>
        /// Gets the libraryId given the name and version, based on the provider.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <param name="providerId"></param>
        /// <returns></returns>
        public string GetLibraryId(string name, string version, string providerId)
        {
            return GetSchemeForProvider(providerId).GetLibraryId(name, version);
        }

        /// <summary>
        /// Returns whether the given library ID is of a valid form for the given provider
        /// </summary>
        public bool IsWellFormedLibraryId(string libraryId, string providerId)
        {
            return GetSchemeForProvider(providerId).IsValidLibraryId(libraryId);
        }

        private ILibraryNamingScheme GetSchemeForProvider(string providerId)
        {
            lock (_syncObject)
            {
                return !string.IsNullOrEmpty(providerId ) && _perProviderNamingScheme.TryGetValue(providerId, out ILibraryNamingScheme scheme)
                    ? scheme :
                    _default;
            }
        }
    }
}
