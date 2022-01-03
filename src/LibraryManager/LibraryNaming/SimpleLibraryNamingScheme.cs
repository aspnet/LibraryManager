// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Web.LibraryManager.LibraryNaming
{
    /// <summary>
    /// A versionless library naming scheme which treats libraryId as the library name.
    /// </summary>
    class SimpleLibraryNamingScheme : ILibraryNamingScheme
    {
        /// <inheritDoc />
        public string GetLibraryId(string name, string version)
        {
            return name ?? string.Empty;
        }

        /// <inheritDoc />
        public (string Name, string Version) GetLibraryNameAndVersion(string libraryId)
        {
            return (libraryId ?? string.Empty, string.Empty);
        }

        /// <inheritdoc />
        public bool IsValidLibraryId(string libraryId)
        {
            return !string.IsNullOrEmpty(libraryId);
        }
    }
}
