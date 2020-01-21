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
        /// Returns the provider's catalog from the provided Url to cacheFile
        /// </summary>
        /// <param name="url">Url to the provider catalog</param>
        /// <param name="cacheFile">Where to store the provider catalog within the cache</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        Task<string> GetCatalogAsync(string url, string cacheFile, CancellationToken cancellationToken);

        /// <summary>
        /// Returns library metadata from provided Url to cacheFile
        /// </summary>
        /// <param name="url">Url to the library metadata</param>
        /// <param name="cacheFile">Where to store the metadata file within the cache</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        Task<string> GetMetadataAsync(string url, string cacheFile, CancellationToken cancellationToken);
    }
}
