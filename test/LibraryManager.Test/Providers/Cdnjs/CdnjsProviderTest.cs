// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            File.Delete(Path.Combine(_dependencies.GetHostInteractions().CacheDirectory, "cdnjs", "cache.json"));
            TestUtils.DeleteDirectoryWithRetries(_projectFolder);
        }

        [TestMethod]
        public async Task InstallAsync_FullEndToEnd()
        {
            ILibraryCatalog catalog = _provider.GetCatalog();

            // Search for libraries to display in search result
            IReadOnlyList<ILibraryGroup> groups = await catalog.SearchAsync("jquery", 4, CancellationToken.None);
            Assert.AreEqual(4, groups.Count);

            // Show details for selected library
            ILibraryGroup group = groups.FirstOrDefault();
            Assert.AreEqual("jquery", group.DisplayName);
            Assert.IsNotNull(group.Description);

            // Get all libraries in group to display version list
            IEnumerable<string> versions = await group.GetLibraryVersions(CancellationToken.None);
            Assert.IsTrue(versions.Count() >= 67);
            Assert.AreEqual("1.2.3", versions.Last(), "Library version mismatch");

            // Get the library to install
            ILibrary library = await catalog.GetLibraryAsync(group.DisplayName, versions.First(), CancellationToken.None);
            Assert.AreEqual(group.DisplayName, library.Name);

            var desiredState = new LibraryInstallationState
            {
                Name = "jquery",
                Version = "3.1.1",
                ProviderId = "cdnjs",
                DestinationPath = "lib",
                Files = new[] { "jquery.js", "jquery.min.js" }
            };

            // Install library
            ILibraryOperationResult result = await _provider.InstallAsync(desiredState, CancellationToken.None).ConfigureAwait(false);

            foreach (string file in desiredState.Files)
            {
                string absolute = Path.Combine(_projectFolder, desiredState.DestinationPath, file);
                Assert.IsTrue(File.Exists(absolute));
            }

            Assert.IsTrue(result.Success);
            Assert.IsFalse(result.Cancelled);
            Assert.AreSame(desiredState, result.InstallationState);
            Assert.AreEqual(0, result.Errors.Count);
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
            ILibraryOperationResult result = await _provider.InstallAsync(desiredState, CancellationToken.None).ConfigureAwait(false);
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
            ILibraryOperationResult result = await _provider.InstallAsync(desiredState, CancellationToken.None).ConfigureAwait(false);
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
            ILibraryOperationResult result = await _provider.InstallAsync(desiredState, CancellationToken.None).ConfigureAwait(false);
            Assert.IsFalse(result.Success);

            // Unknown exception. We no longer validate ILibraryState at the provider level
            Assert.AreEqual("LIB000", result.Errors[0].Code);
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
            ILibraryOperationResult result = await _provider.InstallAsync(desiredState, CancellationToken.None).ConfigureAwait(false);
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
            ILibraryOperationResult result = await _provider.InstallAsync(desiredState, CancellationToken.None).ConfigureAwait(false);
            Assert.IsFalse(result.Success);
            Assert.AreEqual("LIB018", result.Errors[0].Code);
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
        private void GetCatalog()
        {
            ILibraryCatalog catalog = _provider.GetCatalog();

            Assert.IsNotNull(catalog);
        }

        private string _doc = $@"{{
  ""{ManifestConstants.Version}"": ""1.0"",
  ""{ManifestConstants.Libraries}"": [
    {{
      ""{ManifestConstants.Provider}"": ""cdnjs"",
      ""{ManifestConstants.Library}"": ""jquery@3.1.1"",
      ""{ManifestConstants.Destination}"": ""lib"",
      ""{ManifestConstants.Files}"": [ ""jquery.js"", ""jquery.min.js"" ]
    }},
    {{
      ""{ManifestConstants.Provider}"": ""cdnjs"",
      ""{ManifestConstants.Library}"": ""knockout@3.4.1"",
      ""{ManifestConstants.Destination}"": ""lib"",
      ""{ManifestConstants.Files}"": [ ""knockout-min.js"" ]
    }}
  ]
}}
";
    }
}
