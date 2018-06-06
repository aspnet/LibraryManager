// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Web.LibraryManager
{
    /// <summary>
    /// Represents a unique identifier for a version of a library file 
    /// </summary>
    internal class FileIdentifier
    {
        public string Path { get; }
        public string Version { get; }

        public FileIdentifier(string path, string version)
        {
            Path = path;
            Version = version;
        }
    }

    internal class FileIdentifierComparer : IEqualityComparer<FileIdentifier>
    {
        public bool Equals(FileIdentifier file1, FileIdentifier file2)
        {
            if (file1 == null || file2 == null)
            {
                return false;
            }

            return string.Compare(file1.Path.Replace('\\', '/'), file2.Path.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase) == 0 &&
                   string.Compare(file1.Version, file2.Version, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public int GetHashCode(FileIdentifier obj)
        {
            int hashPath = obj.Path == null ? 0 : obj.Path.GetHashCode();
            int hashVersion = obj.Version == null ? 0 : obj.Version.GetHashCode();

            return hashPath ^ hashVersion;
        }
    }
}
