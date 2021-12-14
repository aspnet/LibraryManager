// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Web.LibraryManager.LibraryNaming
{
    /// <summary>
    /// Allows converting libraryId to name and version and vice versa.
    /// </summary>
    internal interface ILibraryNamingScheme
    {
        /// <summary>
        /// Returns whether the given library identifier matches the naming scheme
        /// </summary>
        /// <param name="libraryId">The library ID to validate.</param>
        /// <returns>Returns true if the library ID matches the naming scheme; false otherwise.</returns>
        /// <remarks>This does not indicate that the library ID is valid, but only that it is well-formed.</remarks>
        bool IsValidLibraryId(string libraryId);

        /// <summary>
        /// Splits libraryId into name and version.
        /// </summary>
        (string Name, string Version) GetLibraryNameAndVersion(string libraryId);

        /// <summary>
        /// Gets the libraryId from name and version.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        string GetLibraryId(string name, string version);
    }
}
