// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Web.LibraryManager.Configuration
{
    internal class Constants
    {
        public const string HttpProxy = "http_proxy";
        public const string HttpProxyUser = "http_proxy.user";
        // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Configuration setting name, not a secret.")]
        public const string HttpProxyPassword = "http_proxy.password";
        public const string HttpProxyBypass = "http_proxy.bypass";

        public const string HttpsProxy = "https_proxy";
        public const string HttpsProxyUser = "https_proxy.user";
        // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Configuration setting name, not a secret.")]
        public const string HttpsProxyPassword = "https_proxy.password";
        public const string HttpsProxyBypass = "https_proxy.bypass";

        public const string ForceTls12 = "forcetls12";
    }
}
