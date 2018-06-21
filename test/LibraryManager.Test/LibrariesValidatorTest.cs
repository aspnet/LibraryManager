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
using Microsoft.Web.LibraryManager.Mocks;
using Microsoft.Web.LibraryManager.Providers.Cdnjs;
using Microsoft.Web.LibraryManager.Providers.FileSystem;
using Microsoft.Web.LibraryManager.Providers.Unpkg;

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
            _dependencies = new Dependencies(_hostInteraction, new CdnjsProviderFactory(), new FileSystemProviderFactory(), new UnpkgProviderFactory());
        }

        [TestMethod]
        public async Task DetectConflictsAsync_ConflictingLibraries_SameVersion()
        {
            string expectedErrorCode = "LIB019";
            string expectedErrorMessage = "Cannot restore. Multiple definitions for libraries: jquery, jquery";
            Manifest manifest = Manifest.FromJson(_docConflictingLibraries_SameVersion, _dependencies);

            IEnumerable<ILibraryOperationResult> conflicts = await LibrariesValidator.GetManifestErrorsAsync(manifest, _dependencies, CancellationToken.None);

            Assert.AreEqual(1, conflicts.Count());
            Assert.IsTrue(conflicts.First().Errors.Count() == 1);
            Assert.AreEqual(conflicts.First().Errors.First().Code, expectedErrorCode);
            Assert.AreEqual(conflicts.First().Errors.First().Message, expectedErrorMessage);
        }

        [TestMethod]
        public async Task DetectConflictsAsync_ConflictingLibraries_DifferentVersion()
        {
            string expectedErrorCode = "LIB019";
            string expectedErrorMessage = "Cannot restore. Multiple definitions for libraries: jquery, jquery";
            Manifest manifest = Manifest.FromJson(_docConflictingLibraries_DifferentVersion, _dependencies);

            IEnumerable<ILibraryOperationResult> conflicts = await LibrariesValidator.GetManifestErrorsAsync(manifest, _dependencies, CancellationToken.None);

            Assert.AreEqual(1, conflicts.Count());
            Assert.IsTrue(conflicts.First().Errors.Count() == 1);
            Assert.AreEqual(conflicts.First().Errors.First().Code, expectedErrorCode);
            Assert.AreEqual(conflicts.First().Errors.First().Message, expectedErrorMessage);
        }

        [TestMethod]
        public async Task DetectConflictsAsync_ConflictingLibraries_DifferentFiles()
        {
            string expectedErrorCode = "LIB019";
            string expectedErrorMessage = "Cannot restore. Multiple definitions for libraries: jquery, jquery";
            Manifest manifest = Manifest.FromJson(_docConflictingLibraries_DifferentFiles, _dependencies);

            IEnumerable<ILibraryOperationResult> conflicts = await LibrariesValidator.GetManifestErrorsAsync(manifest, _dependencies, CancellationToken.None);

            Assert.AreEqual(1, conflicts.Count());
            Assert.IsTrue(conflicts.First().Errors.Count() == 1);
            Assert.AreEqual(conflicts.First().Errors.First().Code, expectedErrorCode);
            Assert.AreEqual(conflicts.First().Errors.First().Message, expectedErrorMessage);
        }

        [TestMethod]
        public async Task DetectConflictsAsync_SameLibrary_SameFiles_DifferentDestination()
        {
            string expectedErrorCode = "LIB019";
            string expectedErrorMessage = "Cannot restore. Multiple definitions for libraries: jquery, jquery";
            Manifest manifest = Manifest.FromJson(_docConflictingLibraries_SameFiles_DifferentDestination, _dependencies);

            IEnumerable<ILibraryOperationResult> conflicts = await LibrariesValidator.GetManifestErrorsAsync(manifest, _dependencies, CancellationToken.None);

            Assert.AreEqual(1, conflicts.Count());
            Assert.IsTrue(conflicts.First().Errors.Count() == 1);
            Assert.AreEqual(conflicts.First().Errors.First().Code, expectedErrorCode);
            Assert.AreEqual(conflicts.First().Errors.First().Message, expectedErrorMessage);
        }

        [TestMethod]
        public async Task GetManifestErrors_ManifestIsNull()
        {
            string expectedErrorCode = "LIB004";
            Manifest manifest = null;

            IEnumerable<ILibraryOperationResult> results = await LibrariesValidator.GetManifestErrorsAsync(manifest, _dependencies, CancellationToken.None);

            Assert.AreEqual(1, results.Count());
            Assert.IsTrue(results.First().Errors.Count() == 1);
            Assert.AreEqual(results.First().Errors.First().Code, expectedErrorCode);
        }

        [TestMethod]
        public async Task GetManifestErrors_ManifestHasUnsupportedVersion()
        {
            string expectedErrorCode = "LIB009";
            Manifest manifest = Manifest.FromJson(_docUnsupportedVersion, _dependencies);

            IEnumerable<ILibraryOperationResult> results = await LibrariesValidator.GetManifestErrorsAsync(manifest, _dependencies, CancellationToken.None);

            Assert.AreEqual(1, results.Count());
            Assert.IsTrue(results.First().Errors.Count() == 1);
            Assert.AreEqual(results.First().Errors.First().Code, expectedErrorCode);
        }

        [TestMethod]
        public async Task GetLibrariesErrors_LibrariesNoProvider()
        {
            string expectedErrorCode = "LIB007";
            Manifest manifest = Manifest.FromJson(_docNoProvider, _dependencies);

            IEnumerable<ILibraryOperationResult> results = await LibrariesValidator.GetManifestErrorsAsync(manifest, _dependencies, CancellationToken.None);

            Assert.AreEqual(1, results.Count());
            Assert.IsTrue(results.First().Errors.Count() == 1);
            Assert.AreEqual(results.First().Errors.First().Code, expectedErrorCode);
        }

        private string _docConflictingLibraries_SameVersion = $@"{{
  ""{ManifestConstants.Version}"": ""1.0"",
  ""{ManifestConstants.Libraries}"": [
    {{
      ""{ManifestConstants.Library}"": ""jquery@3.3.1"",
      ""{ManifestConstants.Provider}"": ""unpkg"",
      ""{ManifestConstants.Destination}"": ""lib""
    }},
    {{
      ""{ManifestConstants.Library}"": ""jquery@3.3.1"",
      ""{ManifestConstants.Provider}"": ""unpkg"",
      ""{ManifestConstants.Destination}"": ""lib""
    }},
  ]
}}
";
        private string _docConflictingLibraries_SameFiles_DifferentDestination = $@"{{
  ""{ManifestConstants.Version}"": ""1.0"",
  ""{ManifestConstants.Libraries}"": [
    {{
      ""{ManifestConstants.Library}"": ""jquery@3.3.1"",
      ""{ManifestConstants.Provider}"": ""unpkg"",
      ""{ManifestConstants.Destination}"": ""lib1"",
      ""{ManifestConstants.Files}"": [ ""jquery.min.js"" ]
    }},
    {{
      ""{ManifestConstants.Library}"": ""jquery@3.3.1"",
      ""{ManifestConstants.Provider}"": ""unpkg"",
      ""{ManifestConstants.Destination}"": ""lib2"",
      ""{ManifestConstants.Files}"": [ ""jquery.min.js"" ]
    }},
  ]
}}
";

        private string _docConflictingLibraries_DifferentVersion = $@"{{
  ""{ManifestConstants.Version}"": ""1.0"",
  ""{ManifestConstants.Libraries}"": [
    {{
      ""{ManifestConstants.Library}"": ""jquery@3.3.1"",
      ""{ManifestConstants.Provider}"": ""unpkg"",
      ""{ManifestConstants.Destination}"": ""lib""
    }},
    {{
      ""{ManifestConstants.Library}"": ""jquery@3.3.0"",
      ""{ManifestConstants.Provider}"": ""unpkg"",
      ""{ManifestConstants.Destination}"": ""lib""
    }},
  ]
}}
";
        private string _docConflictingLibraries_DifferentFiles = $@"{{
  ""{ManifestConstants.Version}"": ""1.0"",
  ""{ManifestConstants.Libraries}"": [
    {{
      ""{ManifestConstants.Library}"": ""jquery@3.3.1"",
      ""{ManifestConstants.Provider}"": ""unpkg"",
      ""{ManifestConstants.Destination}"": ""lib"",
      ""{ManifestConstants.Files}"": [ ""jquery.min.js"" ]
    }},
    {{
      ""{ManifestConstants.Library}"": ""jquery@3.3.1"",
      ""{ManifestConstants.Provider}"": ""unpkg"",
      ""{ManifestConstants.Destination}"": ""lib"",
      ""{ManifestConstants.Files}"": [ ""jquery.js"" ]
    }},
  ]
}}
";

        private string _docDifferentDestination = $@"{{
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
      ""{ManifestConstants.Destination}"": ""lib2"",
      ""{ManifestConstants.Files}"": [ ""jquery.min.js"" ]
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
