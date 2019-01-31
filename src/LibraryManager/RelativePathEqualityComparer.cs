// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.Web.LibraryManager
{
    /// <summary>
    /// A comparer to check for path equality.
    /// It normalizes path to handle cases with '..' and '.' or leading and trailing '/' or '\'
    /// Note: This is a workaround till we have https://github.com/dotnet/corefx/issues/10120
    /// </summary>
    internal class RelativePathEqualityComparer : IEqualityComparer<string>
    {
        private RelativePathEqualityComparer() { }

        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static RelativePathEqualityComparer Instance { get; } = new RelativePathEqualityComparer();

        /// <inheritdoc />
        public bool Equals(string x, string y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            x = NormalizePath(x);
            y = NormalizePath(y);

            return x == y;
        }

        /// <inheritdoc />
        public int GetHashCode(string obj)
        {
            return NormalizePath(obj)?.GetHashCode() ?? 0;
        }

        private string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            // net451 does not have the OSPlatform apis to determine if the OS is windows or not.
            // This also does not handle the fact that MacOS can be configured to be either sensitive or insenstive 
            // to the casing.
            if (Path.DirectorySeparatorChar == '\\')
            {
                // Windows filesystem is case insensistive
#pragma warning disable CA1308 // Normalize strings to uppercase
                               // Reason: we prefer lowercased file paths.
                path = path.ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
            }

            // All paths should be treated as relative paths for comparison purposes.
            // '/abc/def' and 'abc/def' have different meanings in Windows vs linux or mac.
            path = path.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
