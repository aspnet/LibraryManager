// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Web.LibraryManager.Providers.Cdnjs;
using Microsoft.Web.LibraryManager.Providers.Shared;

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
            IEnumerable<string> libraryIds = await group.GetLibraryIdsAsync(CancellationToken.None);
            Assert.IsTrue(libraryIds.Count() >= 67);
            Assert.AreEqual("jquery@1.2.3", libraryIds.Last(), "Library version mismatch");

            // Get the library to install
            ILibrary library = await catalog.GetLibraryAsync(libraryIds.First(), CancellationToken.None);
            Assert.AreEqual(group.DisplayName, library.Name);

            var desiredState = new LibraryInstallationState
            {
                LibraryId = "jquery@3.1.1",
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
                LibraryId = "*&(}:@3.1.1",
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
            string providerId = "cdnjs";
            string libraryId = "jquery@1.2.3";
            string destinationPath = "lib";

            var manifest = Manifest.FromJson("{}", _dependencies);
            manifest.AddLibraryValidator(new LibrariesValidator(_dependencies, manifest.DefaultDestination, manifest.DefaultProvider));

            // Install library
            IEnumerable<ILibraryOperationResult> results = await manifest.InstallLibraryAsync(libraryId, providerId, null, destinationPath, CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(results.Count() == 1);
            Assert.IsTrue(results.FirstOrDefault().Success);

            foreach (string file in new[] { "jquery.js", "jquery.min.js" })
            {
                string absolute = Path.Combine(_projectFolder, destinationPath, file);
                Assert.IsTrue(File.Exists(absolute));
            }
        }

        [TestMethod]
        public async Task InstallAsync_NoPathDefined()
        {
            string providerId = "cdnjs";
            string libraryId = "jquery@1.2.3";

            var manifest = Manifest.FromJson("{}", _dependencies);
            manifest.AddLibraryValidator(new LibrariesValidator(_dependencies, manifest.DefaultDestination, manifest.DefaultProvider));

            // Install library
            IEnumerable<ILibraryOperationResult> results = await manifest.InstallLibraryAsync(libraryId, providerId, null, null, CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(results.Count() == 1);
            Assert.IsFalse(results.FirstOrDefault().Success);
            Assert.AreEqual("LIB005", results.FirstOrDefault().Errors.First().Code);
        }

        [TestMethod]
        public async Task InstallAsync_NoProviderDefined()
        {
            string destinationPath = "lib";
            string libraryId = "jquery@1.2.3";

            var manifest = Manifest.FromJson("{}", _dependencies);
            manifest.AddLibraryValidator(new LibrariesValidator(_dependencies, manifest.DefaultDestination, manifest.DefaultProvider));

            // Install library
            IEnumerable<ILibraryOperationResult> results = await manifest.InstallLibraryAsync(libraryId, null, null, destinationPath, CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(results.Count() == 1);
            Assert.IsFalse(results.FirstOrDefault().Success);
            Assert.AreEqual("LIB007", results.FirstOrDefault().Errors.First().Code);
        }

        [TestMethod]
        public async Task InstallAsync_InvalidLibraryFiles()
        {
            string providerId = "cdnjs";
            string libraryId = "jquery@3.1.1";
            string destinationPath = "lib";
            string[] files = new[] { "file1.txt", "file2.txt" };

            var manifest = Manifest.FromJson("{}", _dependencies);
            manifest.AddLibraryValidator(new LibrariesValidator(_dependencies, manifest.DefaultDestination, manifest.DefaultProvider));

            IEnumerable<ILibraryOperationResult> results = await manifest.InstallLibraryAsync(libraryId, providerId, files, destinationPath, CancellationToken.None);
            Assert.IsTrue(results.Count() == 1);
            Assert.IsFalse(results.First().Success);
            Assert.AreEqual("LIB018", results.First().Errors[0].Code);
        }

        [TestMethod]
        public void GetSuggestedDestination()
        {
            Assert.AreEqual(string.Empty, _provider.GetSuggestedDestination(null));

            ILibrary library = new LibraryManager.Providers.Shared.Library()
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
