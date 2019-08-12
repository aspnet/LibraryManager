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
using Microsoft.Web.LibraryManager.Providers.FileSystem;
using Microsoft.Web.LibraryManager.Providers.Unpkg;
using Microsoft.Web.LibraryManager.Providers.jsDelivr;
using Moq;

namespace Microsoft.Web.LibraryManager.Test
{
    [TestClass]
    public class LibrariesValidatorTest
    {
        private string _cacheFolder;
        private string _projectFolder;
        private IDependencies _dependencies;
        private HostInteraction _hostInteraction;

        [TestInitialize]
        public void Setup()
        {
            _cacheFolder = Environment.ExpandEnvironmentVariables(@"%localappdata%\Microsoft\Library\");
            _projectFolder = Path.Combine(Path.GetTempPath(), "LibraryManager");
            _hostInteraction = new HostInteraction(_projectFolder, _cacheFolder);
            var npmPackageSearch = new Mock<INpmPackageSearch>();
            var packageInfoFactory = new Mock<INpmPackageInfoFactory>();

            _dependencies = new Dependencies(_hostInteraction, new CdnjsProviderFactory(), new FileSystemProviderFactory(),
                new UnpkgProviderFactory(npmPackageSearch.Object, packageInfoFactory.Object), new JsDelivrProviderFactory(npmPackageSearch.Object, packageInfoFactory.Object));
            LibraryIdToNameAndVersionConverter.Instance.Reinitialize(_dependencies);
        }

        [TestMethod]
        public async Task DetectConflictsAsync_ConflictingFiles_SameDestination()
        {
            string expectedErrorCode = "LIB016";
            string expectedErrorMessage = "Conflicting file \"lib\\package.json\" found in more than one library: jquery, d3";
            var manifest = Manifest.FromJson(_docDifferentLibraries_SameFiles_SameLocation, _dependencies);

            IEnumerable<ILibraryOperationResult> conflicts = await LibrariesValidator.GetManifestErrorsAsync(manifest, _dependencies, CancellationToken.None);
            var conflictsList = conflicts.ToList();

            Assert.AreEqual(1, conflictsList.Count);
            Assert.IsTrue(conflictsList[0].Errors.Count == 1);
            Assert.AreEqual(conflictsList[0].Errors[0].Code, expectedErrorCode);
            Assert.AreEqual(expectedErrorMessage, conflictsList[0].Errors[0].Message);
        }

        [TestMethod]
        public async Task DetectConflictsAsync_ConflictingFiles_DifferentDestinations()
        {
            var manifest = Manifest.FromJson(_docDifferentLibraries_SameFiles_DifferentLocation, _dependencies);

            IEnumerable<ILibraryOperationResult> conflicts = await LibrariesValidator.GetManifestErrorsAsync(manifest, _dependencies, CancellationToken.None);

            Assert.IsTrue(conflicts.All(c => c.Success));
        }

        [TestMethod]
        public async Task DetectConflictsAsync_SameLibrary_DifferentDestinations()
        {
            string expectedErrorCode = "LIB019";
            string expectedErrorMessage = "Cannot restore. Multiple definitions for libraries: jquery";
            var manifest = Manifest.FromJson(_docSameLibrary_DifferentDestination, _dependencies);

            IEnumerable<ILibraryOperationResult> results = await LibrariesValidator.GetManifestErrorsAsync(manifest, _dependencies, CancellationToken.None);

            var conflictsList = results.ToList();
            Assert.AreEqual(1, conflictsList.Count);
            Assert.IsTrue(conflictsList[0].Errors.Count == 1);
            Assert.AreEqual(conflictsList[0].Errors[0].Code, expectedErrorCode);
            Assert.AreEqual(conflictsList[0].Errors[0].Message, expectedErrorMessage);
        }

        [TestMethod]
        public async Task DetectConflictsAsync_SameLibrary_DifferentProviders()
        {
            string expectedErrorCode = "LIB019";
            string expectedErrorMessage = "Cannot restore. Multiple definitions for libraries: jquery";
            var manifest = Manifest.FromJson(_docSameLibrary_DifferentProviders, _dependencies);

            IEnumerable<ILibraryOperationResult> results = await LibrariesValidator.GetManifestErrorsAsync(manifest, _dependencies, CancellationToken.None);

            var conflictsList = results.ToList();
            Assert.AreEqual(1, conflictsList.Count);
            Assert.IsTrue(conflictsList[0].Errors.Count == 1);
            Assert.AreEqual(conflictsList[0].Errors[0].Code, expectedErrorCode);
            Assert.AreEqual(conflictsList[0].Errors[0].Message, expectedErrorMessage);
        }

        [TestMethod]
        public async Task DetectConflictsAsync_SameLibrary_DifferentVersions_DifferentFiles()
        {
            string expectedErrorCode = "LIB019";
            string expectedErrorMessage = "Cannot restore. Multiple definitions for libraries: jquery";
            var manifest = Manifest.FromJson(_docSameLibrary_DifferentVersions_DifferentFiles, _dependencies);

            IEnumerable<ILibraryOperationResult> results = await LibrariesValidator.GetManifestErrorsAsync(manifest, _dependencies, CancellationToken.None);

            var conflictsList = results.ToList();
            Assert.AreEqual(1, conflictsList.Count);
            Assert.IsTrue(conflictsList[0].Errors.Count == 1);
            Assert.AreEqual(conflictsList[0].Errors[0].Code, expectedErrorCode);
            Assert.AreEqual(conflictsList[0].Errors[0].Message, expectedErrorMessage);
        }

        [TestMethod]
        public async Task GetManifestErrors_ManifestIsNull()
        {
            string expectedErrorCode = "LIB004";
            Manifest manifest = null;

            IEnumerable<ILibraryOperationResult> results = await LibrariesValidator.GetManifestErrorsAsync(manifest, _dependencies, CancellationToken.None);

            var resultsList = results.ToList();
            Assert.AreEqual(1, resultsList.Count);
            Assert.IsTrue(resultsList[0].Errors.Count == 1);
            Assert.AreEqual(resultsList[0].Errors[0].Code, expectedErrorCode);
        }

        [TestMethod]
        public async Task GetManifestErrors_ManifestHasUnsupportedVersion()
        {
            string expectedErrorCode = "LIB009";
            var manifest = Manifest.FromJson(_docUnsupportedVersion, _dependencies);

            IEnumerable<ILibraryOperationResult> results = await LibrariesValidator.GetManifestErrorsAsync(manifest, _dependencies, CancellationToken.None);

            var resultsList = results.ToList();
            Assert.AreEqual(1, resultsList.Count);
            Assert.IsTrue(resultsList[0].Errors.Count == 1);
            Assert.AreEqual(resultsList[0].Errors[0].Code, expectedErrorCode);
        }

        [TestMethod]
        public async Task GetLibrariesErrors_LibrariesNoProvider()
        {
            string expectedErrorCode = "LIB007";
            var manifest = Manifest.FromJson(_docNoProvider, _dependencies);

            IEnumerable<ILibraryOperationResult> results = await LibrariesValidator.GetManifestErrorsAsync(manifest, _dependencies, CancellationToken.None);

            var resultsList = results.ToList();
            Assert.AreEqual(1, resultsList.Count);
            Assert.IsTrue(resultsList[0].Errors.Count == 1);
            Assert.AreEqual(resultsList[0].Errors[0].Code, expectedErrorCode);
        }

        private string _docDifferentLibraries_SameFiles_SameLocation = $@"{{
  ""{ManifestConstants.Version}"": ""1.0"",
  ""{ManifestConstants.Libraries}"": [
    {{
      ""{ManifestConstants.Library}"": ""jquery@3.1.1"",
      ""{ManifestConstants.Provider}"": ""unpkg"",
      ""{ManifestConstants.Destination}"": ""lib"",
      ""{ManifestConstants.Files}"": [ ""package.json"" ]
    }},
    {{
      ""{ManifestConstants.Library}"": ""d3@2.1.3"",
      ""{ManifestConstants.Provider}"": ""unpkg"",
      ""{ManifestConstants.Destination}"": ""lib"",
      ""{ManifestConstants.Files}"": [ ""package.json"" ]
    }},
  ]
}}
";
        private string _docDifferentLibraries_SameFiles_DifferentLocation = $@"{{
  ""{ManifestConstants.Version}"": ""1.0"",
  ""{ManifestConstants.Libraries}"": [
    {{
      ""{ManifestConstants.Library}"": ""jquery@3.1.1"",
      ""{ManifestConstants.Provider}"": ""unpkg"",
      ""{ManifestConstants.Destination}"": ""lib1"",
      ""{ManifestConstants.Files}"": [ ""package.json"" ]
    }},
    {{
      ""{ManifestConstants.Library}"": ""d3@2.1.3"",
      ""{ManifestConstants.Provider}"": ""unpkg"",
      ""{ManifestConstants.Destination}"": ""lib2"",
      ""{ManifestConstants.Files}"": [ ""package.json"" ]
    }},
  ]
}}
";

        private string _docSameLibrary_DifferentVersions_DifferentFiles = $@"{{
  ""{ManifestConstants.Version}"": ""1.0"",
  ""{ManifestConstants.Libraries}"": [
    {{
      ""{ManifestConstants.Library}"": ""jquery@3.1.1"",
      ""{ManifestConstants.Provider}"": ""cdnjs"",
      ""{ManifestConstants.Destination}"": ""lib"",
      ""{ManifestConstants.Files}"": [ ""jquery.min.js"" ]
    }},
    {{
      ""{ManifestConstants.Library}"": ""jquery@2.2.1"",
      ""{ManifestConstants.Provider}"": ""cdnjs"",
      ""{ManifestConstants.Destination}"": ""lib"",
      ""{ManifestConstants.Files}"": [ ""jquery.js"" ]
    }},
  ]
}}
";

        private string _docSameLibrary_DifferentDestination = $@"{{
  ""{ManifestConstants.Version}"": ""1.0"",
  ""{ManifestConstants.Libraries}"": [
    {{
      ""{ManifestConstants.Library}"": ""jquery@3.1.1"",
      ""{ManifestConstants.Provider}"": ""cdnjs"",
      ""{ManifestConstants.Destination}"": ""lib"",
      ""{ManifestConstants.Files}"": [ ""jquery.min.js"" ]
    }},
    {{
      ""{ManifestConstants.Library}"": ""jquery@3.2.1"",
      ""{ManifestConstants.Provider}"": ""cdnjs"",
      ""{ManifestConstants.Destination}"": ""lib2"",
      ""{ManifestConstants.Files}"": [ ""jquery.min.js"" ]
    }},
  ]
}}
";
        private string _docSameLibrary_DifferentProviders = $@"{{
  ""{ManifestConstants.Version}"": ""1.0"",
  ""{ManifestConstants.Libraries}"": [
    {{
      ""{ManifestConstants.Library}"": ""jquery@3.1.1"",
      ""{ManifestConstants.Provider}"": ""cdnjs"",
      ""{ManifestConstants.Destination}"": ""lib"",
      ""{ManifestConstants.Files}"": [ ""jquery.min.js"" ]
    }},
    {{
      ""{ManifestConstants.Library}"": ""jquery@3.1.1"",
      ""{ManifestConstants.Provider}"": ""unpkg"",
      ""{ManifestConstants.Destination}"": ""lib2"",
      ""{ManifestConstants.Files}"": [ ""jquery.js"" ]
    }},
  ]
}}
";

        private string _docInvalidSource = $@"{{
  ""{ManifestConstants.Version}"": ""1.0"",
  ""{ManifestConstants.Libraries}"": [
    {{
      ""{ManifestConstants.Library}"": ""jquery@3.1.1"",
      ""{ManifestConstants.Provider}"": ""cdnjs"",
      ""{ManifestConstants.Destination}"": ""lib"",
      ""{ManifestConstants.Files}"": [ ""jquery.min.js"" ]
    }},
    {{
      ""{ManifestConstants.Library}"": ""../path/to/file.txt"",
      ""{ManifestConstants.Provider}"": ""filesystem"",
      ""{ManifestConstants.Destination}"": ""lib"",
      ""{ManifestConstants.Files}"": [ ""file.txt"" ]
    }},
    {{
      ""{ManifestConstants.Library}"": ""jquery@2.2.1"",
      ""{ManifestConstants.Provider}"": ""cdnjs"",
      ""{ManifestConstants.Destination}"": ""lib2"",
      ""{ManifestConstants.Files}"": [ ""jquery.min.js"" ]
    }},
  ]
}}
";
        private string _docUnsupportedVersion = $@"{{
  ""{ManifestConstants.Version}"": ""2.0"",
  ""{ManifestConstants.Libraries}"": [
    {{
      ""{ManifestConstants.Library}"": ""jquery@3.1.1"",
      ""{ManifestConstants.Provider}"": ""cdnjs"",
      ""{ManifestConstants.Destination}"": ""lib"",
      ""{ManifestConstants.Files}"": [ ""jquery.min.js"" ]
    }}
  ]
}}
";
        private string _docNoProvider = $@"{{
  ""{ManifestConstants.Version}"": ""1.0"",
  ""{ManifestConstants.Libraries}"": [
    {{
      ""{ManifestConstants.Library}"": ""jquery@3.1.1"",
      ""{ManifestConstants.Destination}"": ""lib"",
      ""{ManifestConstants.Files}"": [ ""jquery.min.js"" ]
    }}
  ]
}}
";
    }
}
