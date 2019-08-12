// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.LibraryNaming;
using Microsoft.Web.LibraryManager.Mocks;
using Microsoft.Web.LibraryManager.Providers.Cdnjs;
using Microsoft.Web.LibraryManager.Providers.FileSystem;
using Microsoft.Web.LibraryManager.Providers.Unpkg;
using Moq;

namespace Microsoft.Web.LibraryManager.Test
{
    [TestClass]
    public class LibraryIdToNameAndVersionConverterTest
    {
        private string _filePath;
        private string _cacheFolder;
        private string _projectFolder;
        private IDependencies _dependencies;
        private HostInteraction _hostInteraction;

        [TestInitialize]
        public void Setup()
        {
            _cacheFolder = Environment.ExpandEnvironmentVariables(@"%localappdata%\Microsoft\Library\");
            _projectFolder = Path.Combine(Path.GetTempPath(), "LibraryManager");
            _filePath = Path.Combine(_projectFolder, "libman.json");

            _hostInteraction = new HostInteraction(_projectFolder, _cacheFolder);
            var npmPackageSearch = new Mock<INpmPackageSearch>();
            var packageInfoFactory = new Mock<INpmPackageInfoFactory>();

            _dependencies = new Dependencies(_hostInteraction, new CdnjsProviderFactory(), new FileSystemProviderFactory(), new UnpkgProviderFactory(npmPackageSearch.Object, packageInfoFactory.Object));
            LibraryIdToNameAndVersionConverter.Instance.Reinitialize(_dependencies);
        }

        [DataTestMethod]
        [DataRow("jquery@3.3.1", "cdnjs" ,"jquery", "3.3.1")]
        [DataRow("@angular/cli@1.0.0", "unpkg","@angular/cli", "1.0.0")]
        [DataRow("@MyLibraryWithoutVersion", "cdnjs", "@MyLibraryWithoutVersion", "")]
        [DataRow("Library@Version", "filesystem", "Library@Version", "")]
        [DataRow("Partial@", "cdnjs", "Partial", "")]
        [DataRow("Partial@Version", "unknown", "Partial@Version", "")]  // Default is simple naming scheme
        public void GetLibraryNameAndVersion(string libraryId, string providerId, string name, string version)
        {
            (string actualName, string actualVersion) = LibraryIdToNameAndVersionConverter.Instance.GetLibraryNameAndVersion(libraryId, providerId);

            Assert.AreEqual(name, actualName);
            Assert.AreEqual(version, actualVersion);
        }

        [DataTestMethod]
        [DataRow("jquery@3.3.1", "cdnjs", "jquery", "3.3.1")]
        [DataRow("@angular/cli@1.0.0", "unpkg", "@angular/cli", "1.0.0")]
        [DataRow("@MyLibraryWithoutVersion", "cdnjs", "@MyLibraryWithoutVersion", "")]
        [DataRow("Library@Version", "filesystem", "Library@Version", "")]
        [DataRow("Partial@", "cdnjs", "Partial@", "")]
        [DataRow("Partial@Version", "unknown", "Partial@Version", "")]  // Default is simple naming scheme
        public void GetLibraryId(string libraryId, string providerId, string name, string version)
        {
            string actualLibraryId = LibraryIdToNameAndVersionConverter.Instance.GetLibraryId(name, version, providerId);

            Assert.AreEqual(libraryId, actualLibraryId);
        }
    }
}
