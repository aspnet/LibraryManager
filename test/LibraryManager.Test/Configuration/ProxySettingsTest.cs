// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Configuration;
using Microsoft.Web.LibraryManager.Contracts.Configuration;
using Moq;

namespace Microsoft.Web.LibraryManager.Test.Configuration
{
    [TestClass]
    public class ProxySettingsTest
    {
        [TestMethod]
        public void GetProxy_NoSettings_ReturnsNullProxy()
        {
            var mockSettings = new Mock<ISettings>();
            string outValue = string.Empty;
            mockSettings.Setup(s => s.TryGetValue(It.IsAny<string>(), out outValue))
                        .Returns(false);

            var ut = new ProxySettings(mockSettings.Object);
            IWebProxy result = ut.GetProxy(new Uri("http://test"));

            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetProxy_UserSettingOnly_ReturnsNullProxy()
        {
            var mockSettings = new Mock<ISettings>();
            string outValue = "proxyUser";
            mockSettings.Setup(s => s.TryGetValue(It.IsAny<string>(), out outValue))
                        .Returns(false);
            mockSettings.Setup(s => s.TryGetValue(It.Is<string>(v => v.Equals("http_proxy.user")), out outValue))
                        .Returns(true);
            mockSettings.Setup(s => s.TryGetValue(It.Is<string>(v => v.Equals("http_proxy.password")), out outValue))
                        .Returns(true);

            // verify that the mock is set up to return a value for the proxy credentials
            Assert.IsTrue(mockSettings.Object.TryGetValue("http_proxy.user", out _));

            var ut = new ProxySettings(mockSettings.Object);
            IWebProxy result = ut.GetProxy(new Uri("http://test"));

            Assert.IsNull(result);
        }


        [TestMethod]
        public void GetProxy_NoUserSpecified_ReturnsProxyWithNullCredentials()
        {
            var mockSettings = new Mock<ISettings>();
            string outValue = "proxyValue";
            mockSettings.Setup(s => s.TryGetValue(It.Is<string>(v => v.Equals("http_proxy")), out outValue))
                        .Returns(true);

            var ut = new ProxySettings(mockSettings.Object);
            IWebProxy result = ut.GetProxy(new Uri("http://test"));

            Assert.IsNotNull(result);
            Assert.AreEqual(new Uri("http://proxyValue/"), result.GetProxy(new Uri("http://test")));
            Assert.IsNull(result.Credentials);
        }

        [TestMethod]
        public void GetProxy_NoPasswordSpecified_ReturnsProxyWithNulLCredentials()
        {
            var mockSettings = new Mock<ISettings>();
            string proxyServer = "http://proxyValue/";
            string proxyUser = "proxyUser";
            mockSettings.Setup(s => s.TryGetValue(It.Is<string>(v => v.Equals("http_proxy")), out proxyServer))
                        .Returns(true);
            mockSettings.Setup(s => s.TryGetValue(It.Is<string>(v => v.Equals("http_proxy.user")), out proxyUser))
                        .Returns(true);
            mockSettings.Setup(s => s.TryGetEncryptedValue(It.IsAny<string>(), out proxyUser))
                        .Returns(false);

            var ut = new ProxySettings(mockSettings.Object);
            IWebProxy result = ut.GetProxy(new Uri("http://test"));

            Assert.IsNotNull(result);
            Assert.AreEqual(new Uri("http://proxyValue/"), result.GetProxy(new Uri("http://test")));
            Assert.IsNull(result.Credentials);
        }

        [TestMethod]
        public void GetProxy_AllValuesSpecified_ReturnsProxyWithCredentials()
        {
            var mockSettings = new Mock<ISettings>();
            string proxyServer = "http://proxyValue/";
            string proxyUser = "proxyUser";
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="test value; not a real secret")]
            string proxyPassword = "proxyPassword";
            mockSettings.Setup(s => s.TryGetValue(It.Is<string>(v => v.Equals("http_proxy")), out proxyServer))
                        .Returns(true);
            mockSettings.Setup(s => s.TryGetValue(It.Is<string>(v => v.Equals("http_proxy.user")), out proxyUser))
                        .Returns(true);
            mockSettings.Setup(s => s.TryGetEncryptedValue(It.IsAny<string>(), out proxyPassword))
                        .Returns(true);

            var ut = new ProxySettings(mockSettings.Object);
            IWebProxy result = ut.GetProxy(new Uri("http://test"));

            Assert.IsNotNull(result);
            Assert.AreEqual(new Uri("http://proxyValue/"), result.GetProxy(new Uri("http://test")));
            Assert.IsNotNull(result.Credentials);
            NetworkCredential cred = result.Credentials.GetCredential(new Uri("http://test"), "");
            Assert.AreEqual(proxyUser, cred.UserName);
            Assert.AreEqual(proxyPassword, cred.Password);
        }

        [TestMethod]
        public void GetProxy_BypassIncludesExactMatch_ReturnsNoProxy()
        {
            var mockSettings = new Mock<ISettings>();
            string proxyServer = "http://proxyValue/";
            string url = "http://test.com/test.js";
            mockSettings.Setup(s => s.TryGetValue(It.Is<string>(v => v.Equals("http_proxy")), out proxyServer))
                        .Returns(true);
            mockSettings.Setup(s => s.TryGetValue(It.Is<string>(v => v.Equals("http_proxy.bypass")), out url))
                        .Returns(true);

            var ut = new ProxySettings(mockSettings.Object);
            IWebProxy result = ut.GetProxy(new Uri(url));

            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetProxy_BypassMatchesHostButNotFile_ReturnsProxy()
        {
            string url = "http://test.com/test.js";
            var mockSettings = new Mock<ISettings>();
            string proxyServer = "http://proxyValue/";
            string proxyBypass = "http://tests.com/notTest.js";
            mockSettings.Setup(s => s.TryGetValue(It.Is<string>(v => v.Equals("http_proxy")), out proxyServer))
                        .Returns(true);
            mockSettings.Setup(s => s.TryGetValue(It.Is<string>(v => v.Equals("http_proxy.bypass")), out proxyBypass))
                        .Returns(true);

            var ut = new ProxySettings(mockSettings.Object);
            IWebProxy result = ut.GetProxy(new Uri(url));

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void GetProxy_BypassMatchesDifferentCase_ReturnsNoProxy()
        {
            string url = "http://test.com/Test.js";
            var mockSettings = new Mock<ISettings>();
            string proxyServer = "http://proxyValue/";
            mockSettings.Setup(s => s.TryGetValue(It.Is<string>(v => v.Equals("http_proxy")), out proxyServer))
                        .Returns(true);
            mockSettings.Setup(s => s.TryGetValue(It.Is<string>(v => v.Equals("http_proxy.bypass")), out url))
                        .Returns(true);

            var ut = new ProxySettings(mockSettings.Object);
            IWebProxy result = ut.GetProxy(new Uri(url.ToLower()));

            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetProxy_BypassHostMatch_ReturnsNoProxy()
        {
            string url = "http://test.com/Test.js";
            var mockSettings = new Mock<ISettings>();
            string proxyServer = "http://proxyValue/";
            string proxyBypass = "http://test.com";
            mockSettings.Setup(s => s.TryGetValue(It.Is<string>(v => v.Equals("http_proxy")), out proxyServer))
                        .Returns(true);
            mockSettings.Setup(s => s.TryGetValue(It.Is<string>(v => v.Equals("http_proxy.bypass")), out proxyBypass))
                        .Returns(true);

            var ut = new ProxySettings(mockSettings.Object);
            IWebProxy result = ut.GetProxy(new Uri(url.ToLower()));

            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetProxy_BypassHostPartialMatch_ReturnsProxy()
        {
            string url = "http://test.com/Test.js";
            var mockSettings = new Mock<ISettings>();
            string proxyServer = "http://proxyValue/";
            string proxyBypass = "http://test";
            mockSettings.Setup(s => s.TryGetValue(It.Is<string>(v => v.Equals("http_proxy")), out proxyServer))
                        .Returns(true);
            mockSettings.Setup(s => s.TryGetValue(It.Is<string>(v => v.Equals("http_proxy.bypass")), out proxyBypass))
                        .Returns(true);

            var ut = new ProxySettings(mockSettings.Object);
            IWebProxy result = ut.GetProxy(new Uri(url.ToLower()));

            // We expect a proxy here because the host is different (test vs test.com)
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void GetProxy_BypassMultipleHostsWithMatch_ReturnsNoProxy()
        {
            string url = "http://test.com/Test.js";
            var mockSettings = new Mock<ISettings>();
            string proxyServer = "http://proxyValue/";
            string proxyBypass = "http://proxyValue;http://test.com";
            mockSettings.Setup(s => s.TryGetValue(It.Is<string>(v => v.Equals("http_proxy")), out proxyServer))
                        .Returns(true);
            mockSettings.Setup(s => s.TryGetValue(It.Is<string>(v => v.Equals("http_proxy.bypass")), out proxyBypass))
                        .Returns(true);

            var ut = new ProxySettings(mockSettings.Object);
            IWebProxy result = ut.GetProxy(new Uri(url.ToLower()));

            Assert.IsNull(result);
        }
    }
}
