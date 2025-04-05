// Licensed to the .NET Foundation under one or more agreements.
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
using Microsoft.Web.LibraryManager.Providers.Cdnjs;

namespace Microsoft.Web.LibraryManager.Test.Providers.Cdnjs
{
    [TestClass]
    public class CdnjsProviderTest
    {
        private string _cacheFolder;
        private string _projectFolder;
        private IDependencies _dependencies;
        private IProvider _provider;

        [TestInitialize]
        public void Setup()
        {
            _cacheFolder = Environment.ExpandEnvironmentVariables(@"%localappdata%\Microsoft\Library\");
            _projectFolder = Path.Combine(Path.GetTempPath(), "LibraryManager");
            var hostInteraction = new HostInteraction(_projectFolder, _cacheFolder);
            _dependencies = new Dependencies(hostInteraction, new CdnjsProviderFactory());
            _provider = _dependencies.GetProvider("cdnjs");
            LibraryIdToNameAndVersionConverter.Instance.EnsureInitialized(_dependencies);

            Directory.CreateDirectory(_projectFolder);
        }

        [TestCleanup]
        public void Cleanup()
        {
            string cacheFile = Path.Combine(_dependencies.GetHostInteractions().CacheDirectory, "cdnjs", "cache.json");
            if (File.Exists(cacheFile))
            {
                File.Delete(cacheFile);
            }
            TestUtils.DeleteDirectoryWithRetries(_projectFolder);
        }

        [TestMethod]
        public async Task InstallAsync_InvalidState()
        {
            var desiredState = new LibraryInstallationState
            {
                Name = "*&(}:",
                Version = "3.1.1",
                ProviderId = "cdnjs",
                DestinationPath = "lib",
                Files = new[] { "jquery.min.js" }
            };

            // Install library
            OperationResult<LibraryInstallationGoalState> result = await _provider.InstallAsync(desiredState, CancellationToken.None).ConfigureAwait(false);
            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public async Task InstallAsync_EmptyFilesArray()
        {
            var desiredState = new LibraryInstallationState
            {
                ProviderId = "cdnjs",
                Name = "jquery",
                Version = "1.2.3",
                DestinationPath = "lib"
            };

            // Install library
            OperationResult<LibraryInstallationGoalState> result = await _provider.InstallAsync(desiredState, CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(result.Success);

            foreach (string file in new[] { "jquery.js", "jquery.min.js" })
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
                ProviderId = "cdnjs",
                Name = "jquery",
                Version = "1.2.3"
            };

            // Install library
            OperationResult<LibraryInstallationGoalState> result = await _provider.InstallAsync(desiredState, CancellationToken.None).ConfigureAwait(false);
            Assert.IsFalse(result.Success);

            Assert.AreEqual("LIB021", result.Errors[0].Code);
        }

        [TestMethod]
        public async Task InstallAsync_NoProviderDefined()
        {
            var desiredState = new LibraryInstallationState
            {
                Name = "jquery",
                Version = "1.2.3",
                DestinationPath = "lib"
            };

            // Install library
            OperationResult<LibraryInstallationGoalState> result = await _provider.InstallAsync(desiredState, CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public async Task InstallAsync_InvalidLibraryFiles()
        {
            var desiredState = new LibraryInstallationState
            {
                Name = "jquery",
                Version = "3.1.1",
                ProviderId = "cdnjs",
                DestinationPath = "lib",
                Files = new[] { "file1.txt", "file2.txt" }
            };

            // Install library
            OperationResult<LibraryInstallationGoalState> result = await _provider.InstallAsync(desiredState, CancellationToken.None).ConfigureAwait(false);
            Assert.IsFalse(result.Success);
            Assert.AreEqual("LIB018", result.Errors[0].Code);
        }

        [TestMethod]
        public async Task InstallAsync_WithGlobPatterns_CorrectlyInstallsAllMatchingFiles()
        {
            var desiredState = new LibraryInstallationState
            {
                Name = "jquery",
                Version = "1.2.3",
                DestinationPath = "lib",
                Files = new[] { "*.js", "!*.min.js" },
            };

            // Verify expansion of Files
            OperationResult<LibraryInstallationGoalState> getGoalState = await _provider.GetInstallationGoalStateAsync(desiredState, CancellationToken.None);
            Assert.IsTrue(getGoalState.Success);
            LibraryInstallationGoalState goalState = getGoalState.Result;
            Assert.AreEqual(1, goalState.InstalledFiles.Count);
            Assert.AreEqual("jquery.js", Path.GetFileName(goalState.InstalledFiles.Keys.First()));

            // Install library
            OperationResult<LibraryInstallationGoalState> result = await _provider.InstallAsync(desiredState, CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public void GetSuggestedDestination()
        {
            Assert.AreEqual(string.Empty, _provider.GetSuggestedDestination(null));

            ILibrary library = new CdnjsLibrary()
            {
                Name = "jquery",
                Version = "3.3.1",
                Files = null
            };

            Assert.AreEqual(library.Name, _provider.GetSuggestedDestination(library));
        }

        [TestMethod]
        public void GetCatalog()
        {
            ILibraryCatalog catalog = _provider.GetCatalog();

            Assert.IsNotNull(catalog);
        }
    }
}
