﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Configuration;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Contracts.Configuration;
using Microsoft.Web.LibraryManager.Helpers;

namespace Microsoft.Web.LibraryManager.Cache
{
    /// <summary>
    /// Helper class to hold the HttpClient instance and send requests to get resources
    /// </summary>
    internal class WebRequestHandler : IWebRequestHandler, IDisposable
    {
        private readonly ConcurrentDictionary<string, HttpClient> _cachedHttpClients = new ConcurrentDictionary<string, HttpClient>();

        public static IWebRequestHandler Instance { get; } = new WebRequestHandler(ProxySettings.Default, Settings.DefaultSettings);
        private readonly ProxySettings _proxySettings;
        private readonly ISettings _settings;

        public WebRequestHandler(ProxySettings proxySettings, ISettings settings)
        {
            _proxySettings = proxySettings;
            _settings = settings;
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
            
#pragma warning disable CA2000 // Dispose objects before losing scope
            var httpMessageHandler = new HttpClientHandler();
#pragma warning restore CA2000 // Dispose objects before losing scope
            if (_settings.TryGetValue(Constants.ForceTls12, out string value) && value.Length > 0)
            {
                httpMessageHandler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            }
            httpMessageHandler.Proxy = _proxySettings.GetProxy(new Uri(url));
            var httpClient = new HttpClient(httpMessageHandler);
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"LibraryManager/{ThisAssembly.AssemblyFileVersion}");

            return httpClient;
        }
    }
}
