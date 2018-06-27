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
            _dependencies = new Dependencies(_hostInteraction, new CdnjsProviderFactory(), new FileSystemProviderFactory());
        }

        [TestMethod]
        public async Task DetectConflictsAsync_ConflictingFiles()
        {
            string expectedErrorCode = "LIB016";
            string expectedErrorMessage = "Conflicting file \"lib\\jquery.js\" found in more than one library: jquery@3.1.1, jquery@2.2.1";
            var manifest = Manifest.FromJson(_docConflictingLibraries, _dependencies);

            IEnumerable<ILibraryOperationResult> conflicts = await LibrariesValidator.GetManifestErrorsAsync(manifest, _dependencies, CancellationToken.None);
            var conflictsList = conflicts.ToList();

            Assert.AreEqual(1, conflictsList.Count);
            Assert.IsTrue(conflictsList[0].Errors.Count == 1);
            Assert.AreEqual(conflictsList[0].Errors[0].Code, expectedErrorCode);
            Assert.AreEqual(conflictsList[0].Errors[0].Message, expectedErrorMessage);
        }

        [TestMethod]
        public async Task DetectConflictsAsync_SameLibraryDifferentFiles()
        {
            Manifest manifest = Manifest.FromJson(_docNoConflictingLibraries, _dependencies);

            IEnumerable<ILibraryOperationResult> results = await LibrariesValidator.GetManifestErrorsAsync(manifest, _dependencies, CancellationToken.None);

            Assert.IsTrue(results.All(c => c.Success));
        }

        [TestMethod]
        public async Task DetectConflictsAsync_SameLibrary_SameFiles_DifferentDestination()
        {
            var manifest = Manifest.FromJson(_docDifferentDestination, _dependencies);

            IEnumerable<ILibraryOperationResult> results = await LibrariesValidator.GetManifestErrorsAsync(manifest, _dependencies, CancellationToken.None);

            Assert.IsTrue(results.All(c => c.Success));
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
            Manifest manifest = Manifest.FromJson(_docUnsupportedVersion, _dependencies);

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

        private string _docConflictingLibraries = $@"{{
  ""{ManifestConstants.Version}"": ""1.0"",
  ""{ManifestConstants.Libraries}"": [
    {{
      ""{ManifestConstants.Library}"": ""jquery@3.1.1"",
      ""{ManifestConstants.Provider}"": ""cdnjs"",
      ""{ManifestConstants.Destination}"": ""lib"",
      ""{ManifestConstants.Files}"": [ ""jquery.js"", ""jquery.min.js"" ]
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

        private string _docNoConflictingLibraries = $@"{{
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
