// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

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
#if NET8_0_OR_GREATER
            return HashCode.Combine(Path, Version);
#else
            int hashPath = Path == null ? 0 : Path.GetHashCode();
            int hashVersion = Version == null ? 0 : Version.GetHashCode();

            return hashPath ^ hashVersion;
#endif
        }
    }
}
