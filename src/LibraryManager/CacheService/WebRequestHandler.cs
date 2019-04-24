// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Configuration;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager
{
    /// <summary>
    /// Helper class to hold the HttpClient instance and send requests to get resources
    /// </summary>
    internal class WebRequestHandler : IWebRequestHandler, IDisposable
    {
        private readonly ConcurrentDictionary<string, HttpClient> _cachedHttpClients = new ConcurrentDictionary<string, HttpClient>();

        public static IWebRequestHandler Instance { get; } = new WebRequestHandler(ProxySettings.Default);
        private readonly ProxySettings _proxySettings;

        public WebRequestHandler(ProxySettings proxySettings)
        {
            _proxySettings = proxySettings;
        }

        public void Dispose()
        {
            foreach (HttpClient item in _cachedHttpClients.Values)
            {
                item.Dispose();
            }

            _cachedHttpClients.Clear();
        }

        public async Task<Stream> GetStreamAsync(string url, CancellationToken cancellationToken)
        {
            try
            {
                var uri = new Uri(url);
                string server = uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);
                HttpClient client = _cachedHttpClients.GetOrAdd(server, (host) => CreateHttpClient(host));
                return await client.GetStreamAsync(new Uri(url)).WithCancellation(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new ResourceDownloadException(url, ex);
            }
        }

        private HttpClient CreateHttpClient(string url)
        {
            var httpMessageHandler = new HttpClientHandler();
            httpMessageHandler.Proxy = _proxySettings.GetProxy(new Uri(url));
            var httpClient = new HttpClient(httpMessageHandler);

            return httpClient;
        }
    }
}
