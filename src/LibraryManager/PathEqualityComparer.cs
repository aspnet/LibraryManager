// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.Web.LibraryManager
{
    /// <summary>
    /// A comparer to check for path equality.
    /// It normalizes path to handle cases with '..' and '.' or leading and trailing '/' or '\'
    /// </summary>
    internal class PathEqualityComparer : IEqualityComparer<string>
    {
        private PathEqualityComparer() { }

        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static PathEqualityComparer Instance { get; } = new PathEqualityComparer();

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
#if NET451
            path.ToLower();
#endif

            return Path.GetFullPath(path)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
