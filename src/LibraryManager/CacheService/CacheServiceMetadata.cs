// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Web.LibraryManager
{
    /// <summary>
    /// Holds information for pair: source, destination for a resource to be cached
    /// </summary>
    internal class CacheServiceMetadata
    {
        public CacheServiceMetadata(string source, string cacheFile)
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
