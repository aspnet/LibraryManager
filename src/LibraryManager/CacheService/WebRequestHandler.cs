// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager
{
    /// <summary>
    /// Helper class to hold the HttpClient instance and send requests to get resources
    /// </summary>
    internal class WebRequestHandler : IWebRequestHandler, IDisposable
    {
        private HttpClient _httpClient;
        private WebRequestHandler()
        {
            HttpClientHandler httpMessageHandler = new HttpClientHandler();
            _httpClient = new HttpClient(httpMessageHandler);
        }

        public static IWebRequestHandler Instance { get; } = new WebRequestHandler();

        public void Dispose()
        {
            ((IDisposable)_httpClient).Dispose();
        }

        public async Task<Stream> GetStreamAsync(string url, CancellationToken cancellationToken)
        {
            try
            {
                return await _httpClient.GetStreamAsync(url).WithCancellation(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new ResourceDownloadException(url, ex);
            }
        }
    }
}
