// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Mocks;
using Microsoft.Web.LibraryManager.Providers.jsDelivr;
using Microsoft.Web.LibraryManager.Providers.Unpkg;
using Moq;

namespace Microsoft.Web.LibraryManager.Test.Providers.JsDelivr
{
    [TestClass]
    public class JsDelivrProviderFactoryTest
    {
        private IHostInteraction _hostInteraction;

        [TestInitialize]
        public void Setup()
        {
            string cacheFolder = Environment.ExpandEnvironmentVariables(@"%localappdata%\Microsoft\Library\");
            string projectFolder = Path.Combine(Path.GetTempPath(), "LibraryManager");
            _hostInteraction = new HostInteraction(projectFolder, cacheFolder);
        }

        [TestMethod]
        public void CreateProvider_Success()
        {
            var npmPackageSearch = new Mock<INpmPackageSearch>();
            var packageInfoFactory = new Mock<INpmPackageInfoFactory>();

            var factory = new JsDelivrProviderFactory(npmPackageSearch.Object, packageInfoFactory.Object);
            IProvider provider = factory.CreateProvider(_hostInteraction);

            Assert.AreSame(_hostInteraction.WorkingDirectory, provider.HostInteraction.WorkingDirectory);
            Assert.AreSame(_hostInteraction.CacheDirectory, provider.HostInteraction.CacheDirectory);
            Assert.IsFalse(string.IsNullOrEmpty(provider.Id));
        }

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void CreateProvider_NullParameter()
        {
            var npmPackageSearch = new Mock<INpmPackageSearch>();
            var packageInfoFactory = new Mock<INpmPackageInfoFactory>();

            var factory = new JsDelivrProviderFactory(npmPackageSearch.Object, packageInfoFactory.Object);
            IProvider provider = factory.CreateProvider(null);
        }
    }
}
