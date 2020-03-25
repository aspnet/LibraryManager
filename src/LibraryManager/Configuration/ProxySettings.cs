// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Web.LibraryManager.Contracts.Configuration;

namespace Microsoft.Web.LibraryManager.Configuration
{
    /// <summary>
    /// Determine the proxy settings to use for an HTTP request
    /// </summary>
    public class ProxySettings
    {
        private readonly ISettings _settings;
        private List<Uri> _bypassUris;

        /// <summary>
        /// Returns a ProxySettings that uses the default configuration file.
        /// </summary>
        public static ProxySettings Default => new ProxySettings(Settings.DefaultSettings);

        /// <summary>
        /// Create a ProxySettings using the specified ISettings instance
        /// </summary>
        /// <param name="settings"></param>
        public ProxySettings(ISettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Get the proxy to use for a given URI
        /// </summary>
        public IWebProxy GetProxy(Uri uri)
        {
            bool bypassProxy = CheckForProxyBypass(uri);

            return bypassProxy ? null : GetUserConfiguredProxy();
        }

        private bool CheckForProxyBypass(Uri uri)
        {
            if (_bypassUris == null)
            {
                _bypassUris = new List<Uri>();
                if (_settings.TryGetValue(Constants.HttpsProxyBypass, out string bypassValue)
                    || _settings.TryGetValue(Constants.HttpProxyBypass, out bypassValue))
                {
                    string[] bypasses = bypassValue.Split(';');
                    foreach (string bypass in bypasses)
                    {
                        _bypassUris.Add(new Uri(bypass));
                    }
                }
            }

            foreach (Uri bypass in _bypassUris)
            {
                if (bypass.IsBaseOf(uri))
                {
                    return true;
                }
            }

            return false;
        }

        private IWebProxy GetUserConfiguredProxy()
        {
            IWebProxy configuredProxy = TryConfigureProxy(Constants.HttpsProxy, Constants.HttpsProxyUser, Constants.HttpsProxyPassword)
                                     ?? TryConfigureProxy(Constants.HttpProxy, Constants.HttpProxyUser, Constants.HttpProxyPassword);

            return configuredProxy;
        }

        private IWebProxy TryConfigureProxy(string proxySettingName, string proxyUserSettingName, string proxyPasswordSettingName)
        {
            IWebProxy configuredProxy = null;

            if (_settings.TryGetValue(proxySettingName, out string proxyAddress))
            {
                configuredProxy = new WebProxy(proxyAddress);

                if (_settings.TryGetValue(proxyUserSettingName, out string proxyUser)
                    && _settings.TryGetEncryptedValue(proxyPasswordSettingName, out string proxyPassword))
                {
                    configuredProxy.Credentials = new NetworkCredential(proxyUser, proxyPassword);
                }
            }

            return configuredProxy;
        }
    }
}
