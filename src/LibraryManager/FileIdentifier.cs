// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Web.LibraryManager
{
    /// <summary>
    /// Represents a unique identifier for a version of a library file 
    /// </summary>
    internal class FileIdentifier : IEquatable<FileIdentifier>
    {
        public string Path { get; }
        public string Version { get; }

        public FileIdentifier(string path, string version)
        {
            Path = path;
            Version = version;
        }

        public bool Equals(FileIdentifier other)
        {
            if (other == null)
            {
                return false;
            }

            return Path.Equals(other.Path, StringComparison.OrdinalIgnoreCase) && 
                   Version.Equals(other.Version, StringComparison.OrdinalIgnoreCase);
        }
    }
}
