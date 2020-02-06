// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Helpers;
using Microsoft.Web.LibraryManager.LibraryNaming;
using Microsoft.Web.LibraryManager.Mocks;
using Microsoft.Web.LibraryManager.Providers.Cdnjs;
using Microsoft.Web.LibraryManager.Providers.FileSystem;
using Microsoft.Web.LibraryManager.Providers.Unpkg;
using Moq;

namespace Microsoft.Web.LibraryManager.Test
{

    [TestClass]
    public class LibraryInstallationStateTest
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

            var npmPackageSearch = new Mock<INpmPackageSearch>();
            var packageInfoFactory = new Mock<INpmPackageInfoFactory>();

            _hostInteraction = new HostInteraction(_projectFolder, _cacheFolder);
            _dependencies = new Dependencies(_hostInteraction, new CdnjsProviderFactory(), new FileSystemProviderFactory(), new UnpkgProviderFactory(npmPackageSearch.Object, packageInfoFactory.Object));
            LibraryIdToNameAndVersionConverter.Instance.Reinitialize(_dependencies);
        }

        [TestMethod]
        public void FromInterface()
        {
            var state = new Mocks.LibraryInstallationState
            {
                ProviderId = "_prov_",
                Name = "_lib_",
                DestinationPath = "_path_",
                Files = new List<string>() { "a", "b" },
            };

            var lis = LibraryInstallationState.FromInterface(state);
            Assert.AreEqual(state.ProviderId, lis.ProviderId);
            Assert.AreEqual(state.Name, lis.Name);
            Assert.AreEqual(state.Version, lis.Version);
            Assert.AreEqual(state.DestinationPath, lis.DestinationPath);
            Assert.AreEqual(state.Files, lis.Files);
        }

        [TestMethod]
        public async Task IsValidAsync_NullState()
        {
            ILibraryInstallationState state = null;

            ILibraryOperationResult result = await state.IsValidAsync(_dependencies);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(result.Errors.Count, 1);
            Assert.AreEqual(result.Errors.First().Code, "LIB999");
        }

        [TestMethod]
        public async Task IsValidAsync_State_HasUnknownProvider()
        {
            var state = new Mocks.LibraryInstallationState
            {
                ProviderId = "_prov_",
                Name = "_lib_",
                DestinationPath = "_path_",
                Files = new List<string>() { "a", "b" },
            };

            ILibraryOperationResult result = await state.IsValidAsync(_dependencies);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(result.Errors.Count, 1);
            Assert.AreEqual(result.Errors.First().Code, "LIB001");
        }

        [TestMethod]
        public async Task IsValidAsync_State_HasNoProvider()
        {
            var state = new Mocks.LibraryInstallationState
            {
                Name = "_lib_",
                DestinationPath = "_path_",
                Files = new List<string>() { "a", "b" },
            };

            ILibraryOperationResult result = await state.IsValidAsync(_dependencies);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(result.Errors.Count, 1);
            Assert.AreEqual(result.Errors.First().Code, "LIB007");
        }

        [TestMethod]
        public async Task IsValidAsync_State_HasNoLibraryId()
        {
            var state = new Mocks.LibraryInstallationState
            {
                ProviderId = "unpkg",
                DestinationPath = "_path_",
                Files = new List<string>() { "a", "b" },
            };

            ILibraryOperationResult result = await state.IsValidAsync(_dependencies);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(result.Errors.Count, 1);
            Assert.AreEqual(result.Errors.First().Code, "LIB006");
        }

        [TestMethod]
        public async Task IsValidAsync_State_HasUnknownLibrary()
        {
            var state = new Mocks.LibraryInstallationState
            {
                ProviderId = "unpkg",
                Name = "_lib_",
                DestinationPath = "_path_",
                Files = new List<string>() { "a", "b" },
            };

            ILibraryOperationResult result = await state.IsValidAsync(_dependencies);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(result.Errors.Count, 1);
            Assert.AreEqual(result.Errors.First().Code, "LIB002");
        }

        [TestMethod]
        public async Task IsValidAsync_State_HasUnknownLibraryFile()
        {
            var state = new Mocks.LibraryInstallationState
            {
                ProviderId = "unpkg",
                Name = "jquery",
                Version = "3.3.1",
                DestinationPath = "_path_",
                Files = new List<string>() { "a", "b" },
            };

            ILibraryOperationResult result = await state.IsValidAsync(_dependencies);

            // IsValidAsync does not validate library files
            // Issue https://github.com/aspnet/LibraryManager/issues/254 should fix that
            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public async Task IsValidAsync_State_FileSystem_LibraryIdHasInvalidPathCharacters()
        {
            var state = new Mocks.LibraryInstallationState
            {
                ProviderId = "filesystem",
                Name = "|lib_",
                DestinationPath = "_path_",
                Files = new List<string>() { "a", "b" },
            };

            ILibraryOperationResult result = await state.IsValidAsync(_dependencies);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(result.Errors.Count, 1);
            Assert.AreEqual(result.Errors.First().Code, "LIB002");
        }

        [TestMethod]
        public async Task IsValidAsync_State_FileSystem_LibraryIdDoesNotExist()
        {
            var state = new Mocks.LibraryInstallationState
            {
                ProviderId = "filesystem",
                Name = "lib",
                DestinationPath = "lib",
            };

            ILibraryOperationResult result = await state.IsValidAsync(_dependencies);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(result.Errors.Count, 1);
            Assert.AreEqual(result.Errors.First().Code, "LIB002");
        }

        [TestMethod]
        public async Task IsValidAsync_State_FileSystem_FilesDoNotExist()
        {
            var state = new Mocks.LibraryInstallationState
            {
                ProviderId = "filesystem",
                Name = "http://glyphlist.azurewebsites.net/img/images/Flag.png",
                DestinationPath = "lib",
                Files = new[] { "foo.png" }
            };

            ILibraryOperationResult result = await state.IsValidAsync(_dependencies);

            // FileSystemProvider supports renaming, therefore validation does not fail
            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public async Task IsValidAsync_State_DestinationPath_HasInvalidCharacters()
        {
            var state = new Mocks.LibraryInstallationState
            {
                ProviderId = "filesystem",
                Name = "http://glyphlist.azurewebsites.net/img/images/Flag.png",
                DestinationPath = "|lib"
            };

            ILibraryOperationResult result = await state.IsValidAsync(_dependencies);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(result.Errors.Count, 1);
            Assert.AreEqual("LIB012", result.Errors.First().Code);
        }
    }
}
