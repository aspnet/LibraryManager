// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager
{
    /// <summary>
    /// Service to manage basic operations on libraries cache
    /// </summary>
    internal class CacheService
    {
        // TO DO: Move these expirations to the provider 
        private readonly int _catalogExpiresAfterDays = 1;
        private readonly int _metadataExpiresAfterDays = 1;
        private readonly int _libraryExpiresAfterDays = 30;
        private IWebRequestHandler _requestHandler;

        public CacheService(IWebRequestHandler requestHandler)
        {
            _requestHandler = requestHandler;
        }

        /// <summary>
        /// Returns the provider's catalog from the provided Url to cacheFile
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cacheFile"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<string> GetCatalogAsync(string url, string cacheFile, CancellationToken cancellationToken)
        {
            return await GetResourceAsync(url, cacheFile, _catalogExpiresAfterDays, cancellationToken);
        }

        /// <summary>
        /// Returns library metadata from provided Url to cacheFile
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cacheFile"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<string> GetMetadataAsync(string url, string cacheFile, CancellationToken cancellationToken)
        {
            return await GetResourceAsync(url, cacheFile, _metadataExpiresAfterDays, cancellationToken);
        }

        /// <summary>
        /// Downloads a resource from specified url to a destination file
        /// </summary>
        /// <param name="url"></param>
        /// <param name="fileName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task DownloadToFileAsync(string url, string fileName, CancellationToken cancellationToken)
        {
            using (Stream libraryStream = await _requestHandler.GetStreamAsync(url, cancellationToken))
            {
                await FileHelpers.SafeWriteToFileAsync(fileName, libraryStream, cancellationToken);
            }
        }

        private async Task<string> GetResourceAsync(string url, string localFile, int expiration, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(localFile) || File.GetLastWriteTime(localFile) < DateTime.Now.AddDays(-expiration))
            {
                await DownloadToFileAsync(url, localFile, cancellationToken).ConfigureAwait(false);
            }

            return await FileHelpers.ReadFileAsTextAsync(localFile, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Refreshes the cache for the given set of files if expired 
        /// </summary>
        /// <param name="librariesCacheMetadata"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task RefreshCacheAsync(IEnumerable<CacheServiceMetadata> librariesCacheMetadata, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<Task> refreshTasks = new List<Task>();

            foreach (CacheServiceMetadata metadata in librariesCacheMetadata)
            {
                if (!File.Exists(metadata.DestinationPath) || File.GetLastWriteTime(metadata.DestinationPath) < DateTime.Now.AddDays(-_libraryExpiresAfterDays))
                {
                    Task readFileTask = DownloadToFileAsync(metadata.Source, metadata.DestinationPath, cancellationToken);
                    await readFileTask;
                    refreshTasks.Add(readFileTask);
                }
            }

            await Task.WhenAll(refreshTasks).ConfigureAwait(false);
        }
    }
}
