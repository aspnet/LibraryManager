// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Web.LibraryManager
{
    /// <summary>
    /// Holds information for pair: source, destination for a resource to be cached
    /// </summary>
    public class CacheFileMetadata
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
    }
}
