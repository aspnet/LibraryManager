// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Contracts.Caching
{
    /// <summary>
    /// Contract for defining the operations for a caching layer
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Gets the contents from the specified URL, or if the request fails then from a locally cached copy
        /// </summary>
        /// <param name="url">The URL to request</param>
        /// <param name="cacheFile">The locally cached file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        Task<string> GetContentsFromUriWithCacheFallbackAsync(string url, string cacheFile, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the contents of a local cache file, or if the file does not exist then requests it from the specified URL
        /// </summary>
        /// <param name="cacheFile">The locally cached file</param>
        /// <param name="url">The URL to request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        /// <exception cref="ResourceDownloadException">Thrown when the file doesn't exist and the resource download fails</exception>
        Task<string> GetContentsFromCachedFileWithWebRequestFallbackAsync(string cacheFile, string url, CancellationToken cancellationToken);
    }
}
