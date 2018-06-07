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

        public override bool Equals(object obj)
        {
            FileIdentifier other = obj as FileIdentifier;

            if (other == null)
            {
                return false;
            }

            return string.Compare(Path.Replace('\\', '/'), other.Path.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase) == 0 &&
                   string.Compare(Version, other.Version, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public override int GetHashCode()
        {
            int hashPath = Path == null ? 0 : Path.GetHashCode();
            int hashVersion = Version == null ? 0 : Version.GetHashCode();

            return hashPath ^ hashVersion;
        }
    }
}
