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

            Directory.CreateDirectory(_projectFolder);
            File.WriteAllText(_filePath, _doc);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Directory.Delete(_projectFolder, true);
        }

        [TestMethod]
        public async Task SaveAsync_Success()
        {
            var manifest = new Manifest(_dependencies);

            IProvider provider = _dependencies.GetProvider("cdnjs");
            var desiredState = new LibraryInstallationState
            {
                LibraryId = "jquery@3.1.1",
                ProviderId = "cdnjs",
                DestinationPath = "lib",
                Files = new[] { "jquery.min.js" }
            };

            ILibraryInstallationResult result = await provider.InstallAsync(desiredState, CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(result.Success);

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
                LibraryId = "jquery@3.1.1",
                ProviderId = "cdnjs",
                DestinationPath = "lib",
                Files = new[] { "jquery.js", "jquery.min.js" }
            };

            manifest.AddLibrary(desiredState);
            await manifest.RestoreAsync(token);

            string file1 = Path.Combine(_projectFolder, "lib", "jquery.js");
            string file2 = Path.Combine(_projectFolder, "lib", "jquery.min.js");
            Assert.IsTrue(File.Exists(file1));
            Assert.IsTrue(File.Exists(file2));

            manifest.Uninstall(desiredState.LibraryId, (file) => { _hostInteraction.DeleteFile(file); });

            Assert.IsFalse(File.Exists(file1));
            Assert.IsFalse(File.Exists(file2));
        }

        [TestMethod]
        public async Task CleanAsync_Success()
        {
            var manifest = new Manifest(_dependencies);
            CancellationToken token = CancellationToken.None;

            IProvider provider = _dependencies.GetProvider("cdnjs");
            var state1 = new LibraryInstallationState
            {
                LibraryId = "jquery@3.1.1",
                ProviderId = "cdnjs",
                DestinationPath = "lib",
                Files = new[] { "jquery.js", "jquery.min.js" }
            };

            var state2 = new LibraryInstallationState
            {
                LibraryId = "knockout@3.4.2",
                ProviderId = "cdnjs",
                DestinationPath = "lib",
                Files = new[] { "knockout-min.js" }
            };

            manifest.AddLibrary(state1);
            manifest.AddLibrary(state2);
            await manifest.RestoreAsync(token);

            string file1 = Path.Combine(_projectFolder, "lib", "jquery.js");
            string file2 = Path.Combine(_projectFolder, "lib", "jquery.min.js");
            string file3 = Path.Combine(_projectFolder, "lib", "knockout-min.js");
            Assert.IsTrue(File.Exists(file1));
            Assert.IsTrue(File.Exists(file2));
            Assert.IsTrue(File.Exists(file3));

            manifest.Clean((file) => { _hostInteraction.DeleteFile(file); });

            Assert.IsFalse(File.Exists(file1));
            Assert.IsFalse(File.Exists(file2));
            Assert.IsFalse(File.Exists(file3));
        }

        [TestMethod]
        public async Task RestoreAsync_PartialSuccess()
        {
            var manifest = Manifest.FromJson(_doc, _dependencies);
            IEnumerable<ILibraryInstallationResult> result = await manifest.RestoreAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual(1, result.Count(v => v.Success));
            Assert.AreEqual(1, result.Count(v => !v.Success));
            Assert.AreEqual("LIB002", result.Last().Errors.First().Code);
        }

        [TestMethod]
        public async Task RestoreAsync_UsingDefaultProvider()
        {
            var manifest = Manifest.FromJson(_docDefaultProvider, _dependencies);
            IEnumerable<ILibraryInstallationResult> result = await manifest.RestoreAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(1, result.Count(v => v.Success));
            Assert.AreEqual(manifest.DefaultProvider, result.First().InstallationState.ProviderId);
        }

        [TestMethod]
        public async Task RestoreAsync_UsingDefaultDestination()
        {
            var manifest = Manifest.FromJson(_docDefaultDestination, _dependencies);
            IEnumerable<ILibraryInstallationResult> result = await manifest.RestoreAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(1, result.Count(v => v.Success));
            Assert.AreEqual(manifest.DefaultDestination, result.First().InstallationState.DestinationPath);
        }

        [TestMethod]
        public async Task RestoreAsync_UsingUnknownProvider()
        {
            var dependencies = new Dependencies(_dependencies.GetHostInteractions());
            var manifest = Manifest.FromJson(_doc, dependencies);
            IEnumerable<ILibraryInstallationResult> result = await manifest.RestoreAsync(CancellationToken.None).ConfigureAwait(false);

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
                LibraryId = "jquery@3.2.1",
                DestinationPath = path,
                Files = new[] { "knockout-min.js" }
            };

            manifest.AddLibrary(state);

            IEnumerable<ILibraryInstallationResult> result = await manifest.RestoreAsync(CancellationToken.None);

            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("LIB008", result.First().Errors.First().Code);
        }

        [TestMethod]
        public async Task RestoreAsync_Cancelled()
        {
            var manifest = Manifest.FromJson(_doc, _dependencies);
            var source = new CancellationTokenSource();
            source.Cancel();
            IEnumerable<ILibraryInstallationResult> result = await manifest.RestoreAsync(source.Token);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.ElementAt(0).Cancelled);
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
            Assert.AreEqual("1.0", manifest.Version);
        }

        [TestMethod]
        public async Task FromFileAsync_PathUndefined()
        {
            var manifest = Manifest.FromJson("{}", _dependencies);

            var state = new LibraryInstallationState
            {
                ProviderId = "cdnjs",
                Files = new[] { "knockout-min.js" }
            };

            manifest.AddLibrary(state);

            IEnumerable<ILibraryInstallationResult> result = await manifest.RestoreAsync(CancellationToken.None);

            Assert.AreEqual(1, result.Count());
            Assert.IsFalse(result.First().Success);
            Assert.IsNotNull(result.First().Errors.FirstOrDefault(e => e.Code == "LIB005"));
            Assert.IsNotNull(result.First().Errors.FirstOrDefault(e => e.Code == "LIB006"));
        }

        [TestMethod]
        public async Task FromFileAsync_ProviderUndefined()
        {
            var manifest = Manifest.FromJson("{}", _dependencies);

            var state = new LibraryInstallationState
            {
                LibraryId = "cdnjs",
                DestinationPath = "lib",
                Files = new[] { "knockout-min.js" }
            };

            manifest.AddLibrary(state);

            IEnumerable<ILibraryInstallationResult> result = await manifest.RestoreAsync(CancellationToken.None);

            Assert.AreEqual(1, result.Count());
            Assert.IsFalse(result.First().Success);
            Assert.IsNotNull(result.First().Errors.FirstOrDefault(e => e.Code == "LIB007"));
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
      ""{ManifestConstants.Library}"": ""../path/to/file.txt"",
      ""{ManifestConstants.Provider}"": ""filesystem"",
      ""{ManifestConstants.Destination}"": ""lib"",
      ""{ManifestConstants.Files}"": [ ""file.txt"" ]
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
    }
}
