// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Mocks;
using Microsoft.Web.LibraryManager.Providers.Cdnjs;
using Microsoft.Web.LibraryManager.Providers.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.LibraryNaming;

namespace Microsoft.Web.LibraryManager.Test
{
    [TestClass]
    public class ManifestTest
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
            _dependencies = new Dependencies(_hostInteraction, new CdnjsProviderFactory(), new FileSystemProviderFactory());

            LibraryIdToNameAndVersionConverter.Instance.Reinitialize(_dependencies);
            Directory.CreateDirectory(_projectFolder);
            File.WriteAllText(_filePath, _doc);
        }

        [TestCleanup]
        public void Cleanup()
        {
            TestUtils.DeleteDirectoryWithRetries(_projectFolder);
        }

        [TestMethod]
        public async Task SaveAsync_Success()
        {
            var manifest = new Manifest(_dependencies);

            IProvider provider = _dependencies.GetProvider("cdnjs");
            var desiredState = new LibraryInstallationState
            {
                Name = "jquery",
                Version = "3.3.1",
                ProviderId = "cdnjs",
                DestinationPath = "lib",
                Files = new[] { "jquery.min.js" }
            };

            ILibraryOperationResult result = await provider.InstallAsync(desiredState, CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(result.Success);

            manifest.AddVersion("1.0");
            manifest.AddLibrary(desiredState);
            await manifest.SaveAsync(_filePath, CancellationToken.None).ConfigureAwait(false);

            Manifest newManifest = await Manifest.FromFileAsync(_filePath, _dependencies, CancellationToken.None).ConfigureAwait(false);

            Assert.IsTrue(File.Exists(_filePath));
            Assert.AreEqual(manifest.Libraries.Count(), newManifest.Libraries.Count());
            Assert.AreEqual(manifest.Version, newManifest.Version);
        }

        [TestMethod]
        public async Task UninstallAsync_Success()
        {
            var manifest = new Manifest(_dependencies);
            CancellationToken token = CancellationToken.None;

            IProvider provider = _dependencies.GetProvider("cdnjs");
            var desiredState = new LibraryInstallationState
            {
                Name = "jquery",
                Version = "3.3.1",
                ProviderId = "cdnjs",
                DestinationPath = "lib",
                Files = new[] { "jquery.js", "jquery.min.js" }
            };

            manifest.AddVersion("1.0");
            manifest.AddLibrary(desiredState);
            IEnumerable<ILibraryOperationResult> results = await manifest.RestoreAsync(token);

            string file1 = Path.Combine(_projectFolder, "lib", "jquery.js");
            string file2 = Path.Combine(_projectFolder, "lib", "jquery.min.js");
            Assert.IsTrue(File.Exists(file1));
            Assert.IsTrue(File.Exists(file2));
            Assert.IsTrue(results.Count() == 1);
            Assert.IsTrue(results.First().Success);

            ILibraryOperationResult uninstallResult = await manifest.UninstallAsync(desiredState.Name, desiredState.Version, (file) => _hostInteraction.DeleteFilesAsync(file, token), token);

            Assert.IsFalse(File.Exists(file1));
            Assert.IsFalse(File.Exists(file2));
            Assert.IsTrue(results.Count() == 1);
            Assert.IsTrue(results.First().Success);
        }

        [TestMethod]
        public async Task CleanAsync_Success()
        {
            var manifest = new Manifest(_dependencies);
            CancellationToken token = CancellationToken.None;

            IProvider provider = _dependencies.GetProvider("cdnjs");
            var state1 = new LibraryInstallationState
            {
                Name = "jquery",
                Version="3.1.1",
                ProviderId = "cdnjs",
                DestinationPath = "lib",
                Files = new[] { "jquery.js", "jquery.min.js" }
            };

            var state2 = new LibraryInstallationState
            {
                Name = "knockout",
                Version= "3.4.2",
                ProviderId = "cdnjs",
                DestinationPath = "lib",
                Files = new[] { "knockout-min.js" }
            };

            manifest.AddVersion("1.0");
            manifest.AddLibrary(state1);
            manifest.AddLibrary(state2);
            await manifest.RestoreAsync(token);

            string file1 = Path.Combine(_projectFolder, "lib", "jquery.js");
            string file2 = Path.Combine(_projectFolder, "lib", "jquery.min.js");
            string file3 = Path.Combine(_projectFolder, "lib", "knockout-min.js");
            Assert.IsTrue(File.Exists(file1));
            Assert.IsTrue(File.Exists(file2));
            Assert.IsTrue(File.Exists(file3));

            IEnumerable<ILibraryOperationResult> results = await manifest.CleanAsync((file) => _hostInteraction.DeleteFilesAsync(file, token), token);

            Assert.IsFalse(File.Exists(file1));
            Assert.IsFalse(File.Exists(file2));
            Assert.IsFalse(File.Exists(file3));
            Assert.IsTrue(results.Count() == 2);
            Assert.IsTrue(results.All(r => r.Success));
        }

        [TestMethod]
        public async Task RestoreAsync_PartialSuccess()
        {
            var manifest = Manifest.FromJson(_doc, _dependencies);
            IEnumerable<ILibraryOperationResult> result = await manifest.RestoreAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual(1, result.Count(v => v.Success));
            Assert.AreEqual(1, result.Count(v => !v.Success));
            Assert.AreEqual("LIB002", result.Last().Errors.First().Code);
        }

        [TestMethod]
        public async Task RestoreAsync_UsingDefaultProvider()
        {
            var manifest = Manifest.FromJson(_docDefaultProvider, _dependencies);
            IEnumerable<ILibraryOperationResult> result = await manifest.RestoreAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual(2, result.Count(v => v.Success));
            Assert.AreEqual(manifest.DefaultProvider, result.First().InstallationState.ProviderId);
        }

        [TestMethod]
        public async Task RestoreAsync_UsingDefaultDestination()
        {
            var manifest = Manifest.FromJson(_docDefaultDestination, _dependencies);
            IEnumerable<ILibraryOperationResult> result = await manifest.RestoreAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(1, result.Count(v => v.Success));
            Assert.AreEqual(manifest.DefaultDestination, result.First().InstallationState.DestinationPath);
        }

        [TestMethod]
        public async Task RestoreAsync_UsingUnknownProvider()
        {
            var dependencies = new Dependencies(_dependencies.GetHostInteractions());
            var manifest = Manifest.FromJson(_doc, dependencies);
            IEnumerable<ILibraryOperationResult> result = await manifest.RestoreAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual(2, result.Count(v => !v.Success));
        }

        [DataTestMethod]
        [DataRow("c:\\foo")]
        [DataRow("../")]
        public async Task RestoreAsync_PathOutsideWorkingDir(string path)
        {
            var manifest = Manifest.FromJson("{}", _dependencies);

            var state = new LibraryInstallationState
            {
                ProviderId = "cdnjs",
                Name = "jquery",
                Version = "3.2.1",
                DestinationPath = path,
                Files = new[] { "core.js" }
            };

            manifest.AddVersion("1.0");
            manifest.AddLibrary(state);

            IEnumerable<ILibraryOperationResult> result = await manifest.RestoreAsync(CancellationToken.None);

            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("LIB008", result.First().Errors.First().Code);
        }

        [ExpectedException(typeof(OperationCanceledException))]
        public async Task RestoreAsync_AllRestoreOperationsCancelled()
        {
            var manifest = Manifest.FromJson(_doc, _dependencies);
            var source = new CancellationTokenSource();
            source.Cancel();
            IEnumerable<ILibraryOperationResult> result = await manifest.RestoreAsync(source.Token);
        }

        [TestMethod]
        public async Task RestorAsync_ConflictingLibraries_Validate()
        {
            var manifest = Manifest.FromJson(_docConflictingLibraries, _dependencies);

            IEnumerable<ILibraryOperationResult> result = await manifest.GetValidationResultsAsync(CancellationToken.None);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Last().Errors.Any(e => e.Code == "LIB019"), "LIB019 error code expected.");
        }

        [TestMethod]
        public async Task RestorAsync_ConflictingLibraries_Restore()
        {
            var manifest = Manifest.FromJson(_docConflictingLibraries, _dependencies);

            IEnumerable<ILibraryOperationResult> results = await manifest.RestoreAsync(CancellationToken.None);

            Assert.AreEqual(2, results.Count());
            Assert.IsTrue(results.All(r =>r.Success));
        }

        [TestMethod]
        public void FromJson_Malformed()
        {
            var manifest = Manifest.FromJson("{", _dependencies);
            Assert.IsNull(manifest);
        }

        [TestMethod]
        public async Task FromFileAsync_FileNotFound()
        {
            Manifest manifest = await Manifest.FromFileAsync(@"c:\file\not\found.json", _dependencies, CancellationToken.None);
            Assert.IsNotNull(manifest);
            Assert.AreEqual(0, manifest.Libraries.Count());
            Assert.IsNull(manifest.Version);
        }

        [TestMethod]
        public async Task FromFileAsync_PathUndefined_Restore()
        {
            var manifest = Manifest.FromJson("{}", _dependencies);

            var state = new LibraryInstallationState
            {
                ProviderId = "cdnjs",
                Files = new[] { "knockout-min.js" }
            };

            manifest.AddVersion("1.0");
            manifest.AddLibrary(state);

            var result = await manifest.RestoreAsync(CancellationToken.None) as List<ILibraryOperationResult>;

            Assert.AreEqual(1, result.Count);
            Assert.IsFalse(result[0].Success);
            Assert.AreEqual(result[0].Errors[0].Code, "LIB002");
        }

        [TestMethod]
        public async Task FromFileAsync_PathUndefined_Validate()
        {
            var manifest = Manifest.FromJson("{}", _dependencies);

            var state = new LibraryInstallationState
            {
                ProviderId = "cdnjs",
                Files = new[] { "knockout-min.js" }
            };

            manifest.AddVersion("1.0");
            manifest.AddLibrary(state);

            var result = await manifest.GetValidationResultsAsync(CancellationToken.None) as List<ILibraryOperationResult>;

            Assert.AreEqual(1, result.Count);
            Assert.IsFalse(result[0].Success);
            Assert.AreEqual(result[0].Errors[0].Code, "LIB006");
        }

        [TestMethod]
        public async Task FromFileAsync_ProviderUndefined_Restore()
        {
            var manifest = Manifest.FromJson("{}", _dependencies);

            var state = new LibraryInstallationState
            {
                Name = "cdnjs",
                Version = "",
                DestinationPath = "lib",
                Files = new[] { "knockout-min.js" }
            };

            manifest.AddVersion("1.0");
            manifest.AddLibrary(state);

            IEnumerable<ILibraryOperationResult> result = await manifest.RestoreAsync(CancellationToken.None);

            Assert.AreEqual(1, result.Count());
            Assert.IsFalse(result.First().Success);
            Assert.IsNotNull(result.First().Errors.FirstOrDefault(e => e.Code == "LIB001"));
        }

        [TestMethod]
        public async Task FromFileAsync_ProviderUndefined_Validate()
        {
            var manifest = Manifest.FromJson("{}", _dependencies);

            var state = new LibraryInstallationState
            {
                Name = "cdnjs",
                Version = "",
                DestinationPath = "lib",
                Files = new[] { "knockout-min.js" }
            };

            manifest.AddVersion("1.0");
            manifest.AddLibrary(state);

            IEnumerable<ILibraryOperationResult> result = await manifest.GetValidationResultsAsync(CancellationToken.None);

            Assert.AreEqual(1, result.Count());
            Assert.IsFalse(result.First().Success);
            Assert.IsNotNull(result.First().Errors.FirstOrDefault(e => e.Code == "LIB007"));
        }

        [TestMethod]
        public async Task InstallLibraryAsync()
        {
            var manifest = Manifest.FromJson("{}", _dependencies);

            // Null LibraryId
            IEnumerable<ILibraryOperationResult> results = await manifest.InstallLibraryAsync(null, null,"cdnjs", null, "wwwroot", CancellationToken.None);
            Assert.IsFalse(results.First().Success);
            Assert.AreEqual(1, results.First().Errors.Count);
            Assert.AreEqual("LIB006", results.First().Errors[0].Code);

            // Empty ProviderId
            results = await manifest.InstallLibraryAsync("jquery", "3.2.1", "", null, "wwwroot", CancellationToken.None);
            Assert.IsFalse(results.First().Success);
            Assert.AreEqual(1, results.First().Errors.Count);
            Assert.AreEqual("LIB007", results.First().Errors[0].Code);

            // Null destination
            results = await manifest.InstallLibraryAsync("jquery", "3.2.1", "cdnjs", null, null, CancellationToken.None);

            Assert.IsFalse(results.First().Success);
            Assert.AreEqual(1, results.First().Errors.Count);
            Assert.AreEqual("LIB005", results.First().Errors[0].Code);


            // Valid Options all files.
            results = await manifest.InstallLibraryAsync("jquery", "3.3.1", "cdnjs", null, "wwwroot", CancellationToken.None);

            Assert.IsTrue(results.First().Success);
            Assert.AreEqual("wwwroot", results.First().InstallationState.DestinationPath);
            Assert.AreEqual("jquery", results.First().InstallationState.Name);
            Assert.AreEqual("3.3.1", results.First().InstallationState.Version);
            Assert.AreEqual("cdnjs", results.First().InstallationState.ProviderId);
            Assert.IsNotNull(results.First().InstallationState.Files);

            // Valid parameters and files.
            var files = new List<string>() { "jquery.min.js" };
            results = await manifest.InstallLibraryAsync("jquery", "2.2.0", "cdnjs", files, "wwwroot2", CancellationToken.None);
            Assert.IsFalse(results.First().Success);
            Assert.AreEqual(1, results.First().Errors.Count);
            Assert.AreEqual("LIB019", results.First().Errors[0].Code);

            // Valid parameters invalid files
            files.Add("abc.js");
            results = await manifest.InstallLibraryAsync("twitter-bootstrap", "4.1.1", "cdnjs", files, "wwwroot3", CancellationToken.None);
            Assert.IsFalse(results.First().Success);
            Assert.AreEqual(1, results.First().Errors.Count);
            Assert.AreEqual("LIB018", results.First().Errors[0].Code);
        }

        [TestMethod]
        public async Task InstallLibraryAsync_SetsDefaultProvider()
        {
            var manifest = Manifest.FromJson(_emptyLibmanJson, _dependencies);
            IEnumerable<ILibraryOperationResult> results = await manifest.InstallLibraryAsync("jquery", "3.2.1", "cdnjs", null, "wwwroot", CancellationToken.None);

            Assert.AreEqual("cdnjs", manifest.DefaultProvider);
            var libraryState = manifest.Libraries.First() as LibraryInstallationState;

            Assert.IsTrue(libraryState.IsUsingDefaultProvider);
        }

        [DataTestMethod]
        [DataRow("1.5")]
        [DataRow("2.0")]
        [DataRow("version")]
        public async Task RestoreAsync_VersionIsNotSupported_Validate(string version)
        {
            var manifest = Manifest.FromJson("{}", _dependencies);

            var state = new LibraryInstallationState
            {
                ProviderId = "cdnjs",
                Name = "jquery",
                Version = "3.2.1",
                DestinationPath = "lib",
                Files = new[] { "core.js" }
            };

            manifest.AddVersion(version);
            manifest.AddLibrary(state);

            IEnumerable<ILibraryOperationResult> result = await manifest.GetValidationResultsAsync(CancellationToken.None);

            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("LIB009", result.First().Errors.First().Code);
        }

        [DataTestMethod]
        [DataRow("1.5")]
        [DataRow("2.0")]
        [DataRow("version")]
        public async Task RestoreAsync_VersionIsNotSupported_Restore(string version)
        {
            var manifest = Manifest.FromJson("{}", _dependencies);

            var state = new LibraryInstallationState
            {
                ProviderId = "cdnjs",
                Name = "jquery",
                Version = "3.2.1",
                DestinationPath = "lib",
                Files = new[] { "core.js" }
            };

            manifest.AddVersion(version);
            manifest.AddLibrary(state);

            List<ILibraryOperationResult> results = await manifest.RestoreAsync(CancellationToken.None) as List<ILibraryOperationResult>;

            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results[0].Success);
        }

        private string _doc = $@"{{
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
    }}
  ]
}}
";

        private string _docDefaultProvider = $@"{{
  ""{ManifestConstants.Version}"": ""1.0"",
  ""{ManifestConstants.DefaultProvider}"": ""cdnjs"",
  ""{ManifestConstants.Libraries}"": [
    {{
      ""{ManifestConstants.Library}"": ""jquery@3.1.1"",
      ""{ManifestConstants.Destination}"": ""lib"",
      ""{ManifestConstants.Files}"": [ ""jquery.js"", ""jquery.min.js"" ]
    }},
    {{
      ""{ManifestConstants.Library}"": ""http://glyphlist.azurewebsites.net/img/images/Flag.png"",
      ""{ManifestConstants.Provider}"": ""filesystem"",
      ""{ManifestConstants.Destination}"": ""lib"",
      ""{ManifestConstants.Files}"": [ ""Flag.png"" ]
    }}
  ]
}}
";
        private string _docDefaultDestination = $@"{{
  ""{ManifestConstants.Version}"": ""1.0"",
  ""{ManifestConstants.DefaultDestination}"": ""lib"",
  ""{ManifestConstants.Libraries}"": [
    {{
      ""{ManifestConstants.Library}"": ""jquery@3.1.1"",
      ""{ManifestConstants.Provider}"": ""cdnjs"",
      ""{ManifestConstants.Files}"": [ ""jquery.js"", ""jquery.min.js"" ]
    }}
  ]
}}
";
        private string _docOldVersionLibrary = $@"{{
  ""{ManifestConstants.Version}"": ""1.0"",
  ""{ManifestConstants.DefaultDestination}"": ""lib"",
  ""{ManifestConstants.Libraries}"": [
    {{
      ""{ManifestConstants.Library}"": ""jquery@2.2.0"",
      ""{ManifestConstants.Provider}"": ""cdnjs"",
      ""{ManifestConstants.Files}"": [ ""jquery.js"", ""jquery.min.js"" ]
    }}
  ]
}}
";
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
        private string _emptyLibmanJson = $@"{{
  ""{ManifestConstants.Version}"": ""1.0"",
  ""{ManifestConstants.Libraries}"": [ ]
}}
";
    }
}
