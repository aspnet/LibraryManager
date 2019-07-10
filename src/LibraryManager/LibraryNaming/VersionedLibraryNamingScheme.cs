// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Web.LibraryManager.LibraryNaming
{
    /// <inheritDoc />
    internal sealed class VersionedLibraryNamingScheme : ILibraryNamingScheme
    {
        private const char Separator = '@';

        /// <summary>
        /// Splits libraryId into name and version using '@' as the split char.
        /// Only the last appearance of '@' is used to split the libraryId.
        /// Note: If libraryId starts with '@', a (name + version) substring will be split after the first '/',
        /// then the name and version will be split by the last '@' in that substring
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

            int indexOfFirstSlash = libraryId.IndexOf('/');
            if (libraryId.StartsWith("@", StringComparison.Ordinal) && indexOfFirstSlash > 0)
            {
                libraryId = libraryId.Substring(indexOfFirstSlash + 1);
            }

            int indexOfAt = libraryId.LastIndexOf(Separator);

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
                : $"{name}{Separator}{version}";

        }
    }
}
