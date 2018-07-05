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
        }

        private readonly object _syncObject = new object();
        private readonly ILibraryNamingScheme _versionedNamingScheme = new VersionedLibraryNamingScheme();
        private readonly ILibraryNamingScheme _simpleNamingScheme = new SimpleLibraryNamingScheme();
        private readonly ILibraryNamingScheme _default;


        private IDependencies _dependencies;
        private Dictionary<string, ILibraryNamingScheme> _perProviderNamingScheme = new Dictionary<string, ILibraryNamingScheme>();

        /// <summary>
        /// Initializes the LibraryNaming Schemes for the manifest.
        /// </summary>
        /// <param name="dependencies"></param>
        public void Initialize (IDependencies dependencies)
        {
            _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));

            lock(_syncObject)
            {
                _perProviderNamingScheme.Clear();

                foreach(IProvider p in _dependencies.Providers)
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
            if (!string.IsNullOrEmpty(providerId) && _perProviderNamingScheme.TryGetValue(providerId, out ILibraryNamingScheme _scheme))
            {
                return _scheme.GetLibraryNameAndVersion(libraryId);
            }

            return _default.GetLibraryNameAndVersion(libraryId);
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
            if (!string.IsNullOrEmpty(providerId) && _perProviderNamingScheme.TryGetValue(providerId, out ILibraryNamingScheme _scheme))
            {
                return _scheme.GetLibraryId(name, version);
            }

            return _default.GetLibraryId(name, version);
        }
    }
}
