// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Web.LibraryManager.Contracts
{
    /// <summary>
    /// Rerpresents the name and version of a provider's library
    /// </summary>
    public class LibraryIdentifier
    {
        /// <summary>
        /// Creates a new library identifier 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        public LibraryIdentifier(string name, string version)
        {
            Name = name;
            Version = version;
        }

        /// <summary>
        /// Name of the library
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Version of the library 
        /// </summary>
        public string Version { get; }
    }
}
