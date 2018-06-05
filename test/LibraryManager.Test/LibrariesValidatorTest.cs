// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
        private Manifest _manifest;

        [TestInitialize]
        public void Setup()
        {
            _cacheFolder = Environment.ExpandEnvironmentVariables(@"%localappdata%\Microsoft\Library\");
            _projectFolder = Path.Combine(Path.GetTempPath(), "LibraryManager");
            _hostInteraction = new HostInteraction(_projectFolder, _cacheFolder);
            _dependencies = new Dependencies(_hostInteraction, new CdnjsProviderFactory(), new FileSystemProviderFactory());
        }

        [TestMethod]
        public void DetectConlictsAsync_ConflictingFiles()
        {
            _manifest = Manifest.FromJson(_docConflictingLibraries, _dependencies);

            var conflictDetector = new LibrariesValidator(_dependencies, _manifest.DefaultDestination, _manifest.DefaultProvider);

            IEnumerable<FileConflict> conflicts = conflictDetector.GetFilesConflicts(_manifest.Libraries, CancellationToken.None);

            Assert.AreEqual(1, conflicts.Count());

            Assert.AreEqual("lib\\jquery.js", conflicts.First().File);

            Assert.AreEqual(2, conflicts.First().Libraries.Count);
        }

        [TestMethod]
        public void DetectConflictsAsync_SameLibraryDifferentFiles()
        {
            _manifest = Manifest.FromJson(_docNoConflictingLibraries, _dependencies);

            var conflictDetector = new LibrariesValidator(_dependencies, _manifest.DefaultDestination, _manifest.DefaultProvider);

            IEnumerable<FileConflict> conflicts = conflictDetector.GetFilesConflicts(_manifest.Libraries, CancellationToken.None);

            Assert.AreEqual(0, conflicts.Count());
        }

        [TestMethod]
        public void DetectConflictsAsync_SameLibrary_SameFiles_DifferentDestination()
        {
            _manifest = Manifest.FromJson(_docDifferentDestination, _dependencies);

            var conflictDetector = new LibrariesValidator(_dependencies, _manifest.DefaultDestination, _manifest.DefaultProvider);

            IEnumerable<FileConflict> conflicts = conflictDetector.GetFilesConflicts(_manifest.Libraries, CancellationToken.None);

            Assert.AreEqual(0, conflicts.Count());
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
      ""{ManifestConstants.Library}"": ""../path/to/file.txt"",
      ""{ManifestConstants.Provider}"": ""filesystem"",
      ""{ManifestConstants.Destination}"": ""lib"",
      ""{ManifestConstants.Files}"": [ ""file.txt"" ]
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
      ""{ManifestConstants.Library}"": ""../path/to/file.txt"",
      ""{ManifestConstants.Provider}"": ""filesystem"",
      ""{ManifestConstants.Destination}"": ""lib"",
      ""{ManifestConstants.Files}"": [ ""file.txt"" ]
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
    }
}
