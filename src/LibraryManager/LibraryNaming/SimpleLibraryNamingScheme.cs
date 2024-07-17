// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
