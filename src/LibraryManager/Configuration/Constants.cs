// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

    }
}
