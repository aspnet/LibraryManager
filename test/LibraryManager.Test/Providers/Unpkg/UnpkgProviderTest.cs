﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.LibraryNaming;
using Microsoft.Web.LibraryManager.Mocks;
using Microsoft.Web.LibraryManager.Providers.Unpkg;

namespace Microsoft.Web.LibraryManager.Test.Providers.Unpkg
{
    [TestClass]
    public class UnpkgProviderTest
    {
        private string _projectFolder;
        private IProvider _provider;

        [TestInitialize]
        public void Setup()
        {
            string cacheFolder = Environment.ExpandEnvironmentVariables(@"%localappdata%\Microsoft\Library\");
            _projectFolder = Path.Combine(Path.GetTempPath(), "LibraryManager");

            var hostInteraction = new HostInteraction(_projectFolder, cacheFolder);

            var requestHandler = new Mocks.WebRequestHandler();
            var npmPackageSearch = new NpmPackageSearch(requestHandler);
            var packageInfoFactory = new NpmPackageInfoFactory(requestHandler);

            var dependencies = new Dependencies(hostInteraction, new UnpkgProviderFactory(npmPackageSearch, packageInfoFactory));
            _provider = dependencies.GetProvider("unpkg");

            LibraryIdToNameAndVersionConverter.Instance.Reinitialize(dependencies);
            Directory.CreateDirectory(_projectFolder);
        }

        [TestCleanup]
        public void Cleanup()
        {
            TestUtils.DeleteDirectoryWithRetries(_projectFolder);
        }

        [TestMethod]
        public async Task InstallAsync_InvalidState()
        {
            var desiredState = new LibraryInstallationState
            {
                Name = "*&(}:",
                Version = "3.3.1",
                ProviderId = "unpkg",
                DestinationPath = "lib",
                Files = new[] { "dist/jquery.min.js" }
            };

            // Install library
            ILibraryOperationResult result = await _provider.InstallAsync(desiredState, CancellationToken.None).ConfigureAwait(false);
            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public async Task InstallAsync_EmptyFilesArray()
        {
            var desiredState = new LibraryInstallationState
            {
                ProviderId = "unpkg",
                Name = "jquery",
                Version = "3.3.1",
                DestinationPath = "lib"
            };

            // Install library
            ILibraryOperationResult result = await _provider.InstallAsync(desiredState, CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(result.Success);

            foreach (string file in new[] { "dist/jquery.js", "dist/jquery.min.js" })
            {
                string absolute = Path.Combine(_projectFolder, desiredState.DestinationPath, file);
                Assert.IsTrue(File.Exists(absolute));
            }
        }

        [TestMethod]
        public async Task InstallAsync_NoPathDefined()
        {
            var desiredState = new LibraryInstallationState
            {
                ProviderId = "unpkg",
                Name = "jquery",
                Version = "3.3.1"
            };

            // Install library
            ILibraryOperationResult result = await _provider.InstallAsync(desiredState, CancellationToken.None).ConfigureAwait(false);
            Assert.IsFalse(result.Success);

            Assert.AreEqual("LIB021", result.Errors[0].Code);
        }

        [TestMethod]
        public async Task InstallAsync_NoProviderDefined()
        {
            var desiredState = new LibraryInstallationState
            {
                Name = "jquery",
                Version = "3.3.1",
                DestinationPath = "lib"
            };

            // Install library
            ILibraryOperationResult result = await _provider.InstallAsync(desiredState, CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public async Task InstallAsync_InvalidLibraryFiles()
        {
            var desiredState = new LibraryInstallationState
            {
                Name = "jquery",
                Version = "3.3.1",
                ProviderId = "unpkg",
                DestinationPath = "lib",
                Files = new[] { "file1.txt", "file2.txt" }
            };

            // Install library
            ILibraryOperationResult result = await _provider.InstallAsync(desiredState, CancellationToken.None).ConfigureAwait(false);
            Assert.IsFalse(result.Success);
            Assert.AreEqual("LIB018", result.Errors[0].Code);
        }

        [TestMethod]
        public async Task InstallAsync_WithGlobPatterns_CorrectlyInstallsAllMatchingFiles()
        {
            var desiredState = new LibraryInstallationState
            {
                Name = "jquery",
                Version = "3.3.1",
                DestinationPath = "lib",
                Files = new[] { "dist/*.js", "!dist/*min*" },
            };

            // Verify expansion of Files
            OperationResult<LibraryInstallationGoalState> getGoalState = await _provider.GetInstallationGoalStateAsync(desiredState, CancellationToken.None);
            Assert.IsTrue(getGoalState.Success);
            LibraryInstallationGoalState goalState = getGoalState.Result;
            // Remove the project folder and "/lib/" from the file paths
            List<string> installedFiles = goalState.InstalledFiles.Keys.Select(f => f.Substring(_projectFolder.Length + 5).Replace("\\", "/")).ToList();
            CollectionAssert.AreEquivalent(new[] { "dist/core.js", "dist/jquery.js", "dist/jquery.slim.js" }, installedFiles);

            // Install library
            ILibraryOperationResult result = await _provider.InstallAsync(desiredState, CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public void GetSuggestedDestination_NullLibrary_ReturnsEmptyString()
        {
            Assert.AreEqual(string.Empty, _provider.GetSuggestedDestination(null));
        }

        [DataTestMethod]
        [DataRow("jquery", "jquery")]
        [DataRow("@angular/cli", "angular/cli")]
        public void GetSuggestedDestination(string libraryName, string expected)
        {
            var library = new UnpkgLibrary()
            {
                Name = libraryName,
                Version = "3.3.1",
                Files = null
            };

            Assert.AreEqual(expected, _provider.GetSuggestedDestination(library));
        }
    }
}
