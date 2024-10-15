// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Web.LibraryManager.Cache
{
    /// <summary>
    /// Holds information for pair: source, destination for a resource to be cached
    /// </summary>
    public class CacheFileMetadata : IEquatable<CacheFileMetadata>
    {
        /// <summary>
        /// Create a new CacheFileMetadata
        /// </summary>
        /// <param name="source">The URI from where the file originated</param>
        /// <param name="cacheFile">The path where the file is to be cached</param>
        public CacheFileMetadata(string source, string cacheFile)
        {
            DestinationPath = cacheFile;
            Source = source;
        }

        /// <summary>
        /// Path for cache file to be written
        /// </summary>
        public string DestinationPath { get; private set; }

        /// <summary>
        /// Source for the cache's contents  
        /// </summary>
        public string Source { get; private set; }

        /// <inheritdoc />
        public bool Equals(CacheFileMetadata other)
        {
            return DestinationPath == other?.DestinationPath && Source == other?.Source;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as CacheFileMetadata);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
#if NET8_0_OR_GREATER
            return DestinationPath.GetHashCode(StringComparison.Ordinal); // this should be a unique identifier
#else
            return DestinationPath.GetHashCode(); // this should be a unique identifier
#endif
        }
    }
}
