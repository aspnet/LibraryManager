// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Web.LibraryManager.LibraryNaming
{
    /// <inheritDoc />
    class VersionedLibraryNamingScheme : ILibraryNamingScheme
    {
        private const char _separator = '@';

        /// <summary>
        /// Splits libraryId into name and version using '@' as the split char.
        /// Only the last appearance of '@' is used to split the libraryId.
        /// Note: If libraryId starts with '@' and has no other occurences of '@',
        /// the entire libraryId is considered to be the name and version is considered to be
        /// empty.
        /// </summary>
        /// <param name="libraryId"></param>
        /// <returns></returns>
        public (string Name, string Version) GetLibraryNameAndVersion(string libraryId)
        {
            string name = string.Empty;
            string version = string.Empty;
            if (string.IsNullOrEmpty(libraryId))
            {
                return (name, version);
            }

            int indexOfAt = libraryId.LastIndexOf(_separator);

            name = libraryId;
            version = string.Empty;

            if (indexOfAt > 0 && indexOfAt < libraryId.TrimEnd().Length - 1)
            {
                name = libraryId.Substring(0, indexOfAt);
                version = libraryId.Substring(indexOfAt + 1);
            }

            return (name, version);
        }

        /// <summary>
        /// Generates a libraryId by concatenating name and version.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public string GetLibraryId(string name, string version)
        {
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(version))
            {
                return string.Empty;
            }

            return string.IsNullOrWhiteSpace(version)
                ? name
                : $"{name}{_separator}{version}";

        }
    }
}
