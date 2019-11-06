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
using Microsoft.Web.LibraryManager.Providers.FileSystem;

namespace Microsoft.Web.LibraryManager.Test.Providers.FileSystem
{
    [TestClass]
    public class FileSystemProviderTest
    {
        private string _file1, _file2, _relativeSrc;
        private string _projectFolder, _configFilePath;
        private IDependencies _dependencies;

        [TestInitialize]
        public void Setup()
        {
            _projectFolder = Path.Combine(Path.GetTempPath(), "LibraryManager\\");
            _configFilePath = Path.Combine(_projectFolder, "libman.json");
            _relativeSrc = Path.Combine(_projectFolder, "folder", "file.txt");

            var hostInteraction = new HostInteraction(_projectFolder, "");
            _dependencies = new Dependencies(hostInteraction, new FileSystemProviderFactory());

            LibraryIdToNameAndVersionConverter.Instance.Reinitialize(_dependencies);

            // Create the files to install
            Directory.CreateDirectory(_projectFolder);
            _file1 = Path.GetTempFileName();
            _file2 = Path.GetTempFileName();
            File.WriteAllText(_file1, "test content");
            File.WriteAllText(_file2, "test content");

            Directory.CreateDirectory(Path.GetDirectoryName(_relativeSrc));
            File.WriteAllText(_relativeSrc, "test content");
        }

        [TestCleanup]
        public void Cleanup()
        {
            File.Delete(_file1);
            File.Delete(_file2);
            TestUtils.DeleteDirectoryWithRetries(_projectFolder);
        }

        [TestMethod]
        public async Task InstallAsync_Success()
        {
            IProvider provider = _dependencies.GetProvider("filesystem");

            var desiredState = new LibraryInstallationState
            {
                ProviderId = "filesystem",
                Name = _file1,
                DestinationPath = "lib",
                Files = new[] { "file1.txt" }
            };

            ILibraryOperationResult result = await provider.InstallAsync(desiredState, CancellationToken.None);
            Assert.IsTrue(result.Success, "Didn't install");

            string copiedFile = Path.Combine(_projectFolder, desiredState.DestinationPath, desiredState.Files[0]);
            Assert.IsTrue(File.Exists(copiedFile), "File1 wasn't copied");

            var manifest = Manifest.FromJson("{}", _dependencies);
            manifest.AddLibrary(desiredState);
            await manifest.SaveAsync(_configFilePath, CancellationToken.None);

            Assert.IsTrue(File.Exists(_configFilePath));
            Assert.AreEqual(File.ReadAllText(copiedFile), "test content");
        }

        [TestMethod]
        public async Task InstallAsync_RelativeFile()
        {
            IProvider provider = _dependencies.GetProvider("filesystem");

            var desiredState = new LibraryInstallationState
            {
                ProviderId = "filesystem",
                Name = "folder/file.txt",
                DestinationPath = "lib",
                Files = new[] { "relative.txt" }
            };

            ILibraryOperationResult result = await provider.InstallAsync(desiredState, CancellationToken.None);
            Assert.IsTrue(result.Success, "Didn't install");

            string copiedFile = Path.Combine(_projectFolder, desiredState.DestinationPath, desiredState.Files[0]);
            Assert.IsTrue(File.Exists(copiedFile), "File1 wasn't copied");
            Assert.IsFalse(result.Cancelled);
            Assert.AreEqual(0, result.Errors.Count);

            var manifest = Manifest.FromJson("{}", _dependencies);
            manifest.AddLibrary(desiredState);
            await manifest.SaveAsync(_configFilePath, CancellationToken.None);

            Assert.IsTrue(File.Exists(_configFilePath));
            Assert.AreEqual(File.ReadAllText(copiedFile), "test content");
        }

        [TestMethod]
        public async Task InstallAsync_AbsoluteFolderFiles()
        {
            string folder = Path.Combine(Path.GetTempPath(), "LibraryManager_test");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "file1.js"), "");
            File.WriteAllText(Path.Combine(folder, "file2.js"), "");
            File.WriteAllText(Path.Combine(folder, "file3.js"), "");

            IProvider provider = _dependencies.GetProvider("filesystem");

            var desiredState = new LibraryInstallationState
            {
                ProviderId = "filesystem",
                Name = folder,
                DestinationPath = "lib",
                Files = new[] { "file1.js", "file2.js" }
            };

            ILibraryOperationResult result = await provider.InstallAsync(desiredState, CancellationToken.None);
            Assert.IsTrue(result.Success, "Didn't install");

            string file1 = Path.Combine(_projectFolder, desiredState.DestinationPath, desiredState.Files[0]);
            string file2 = Path.Combine(_projectFolder, desiredState.DestinationPath, desiredState.Files[1]);
            Assert.IsTrue(File.Exists(file1), "File1 wasn't copied");
            Assert.IsTrue(File.Exists(file2), "File2 wasn't copied");

            Assert.IsFalse(result.Cancelled);
            Assert.AreEqual(0, result.Errors.Count);
        }

        [TestMethod]
        public async Task InstallAsync_RelativeFolderFiles()
        {
            string folder = Path.Combine(Path.GetTempPath(), "LibraryManager_test\\");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "file1.js"), "");
            File.WriteAllText(Path.Combine(folder, "file2.js"), "");
            File.WriteAllText(Path.Combine(folder, "file3.js"), "");

            var origin = new Uri(folder, UriKind.Absolute);
            var current = new Uri(_projectFolder, UriKind.Absolute);
            Uri relativeFolder = current.MakeRelativeUri(origin);

            IProvider provider = _dependencies.GetProvider("filesystem");

            var desiredState = new LibraryInstallationState
            {
                ProviderId = "filesystem",
                Name = relativeFolder.OriginalString,
                DestinationPath = "lib",
                Files = new[] { "file1.js", "file2.js" }
            };

            ILibraryOperationResult result = await provider.InstallAsync(desiredState, CancellationToken.None);
            Assert.IsTrue(result.Success, "Didn't install");

            string file1 = Path.Combine(_projectFolder, desiredState.DestinationPath, desiredState.Files[0]);
            string file2 = Path.Combine(_projectFolder, desiredState.DestinationPath, desiredState.Files[1]);
            Assert.IsTrue(File.Exists(file1), "File1 wasn't copied");
            Assert.IsTrue(File.Exists(file2), "File1 wasn't copied");

            Assert.IsFalse(result.Cancelled);
            Assert.AreEqual(0, result.Errors.Count);
        }

        [TestMethod]
        public async Task InstallAsync_Uri()
        {
            IProvider provider = _dependencies.GetProvider("filesystem");

            var desiredState = new LibraryInstallationState
            {
                ProviderId = "filesystem",
                Name = "https://raw.githubusercontent.com/jquery/jquery/master/src/event.js",
                DestinationPath = "lib",
                Files = new[] { "event.js" }
            };

            ILibraryOperationResult result = await provider.InstallAsync(desiredState, CancellationToken.None);
            Assert.IsTrue(result.Success, "Didn't install");

            string copiedFile = Path.Combine(_projectFolder, desiredState.DestinationPath, desiredState.Files[0]);
            Assert.IsTrue(File.Exists(copiedFile), "File wasn't copied");

            var manifest = Manifest.FromJson("{}", _dependencies);
            manifest.AddLibrary(desiredState);
            await manifest.SaveAsync(_configFilePath, CancellationToken.None);

            Assert.IsTrue(File.Exists(_configFilePath));
            Assert.IsTrue(File.ReadAllText(copiedFile).Length > 1000);
        }

        [TestMethod]
        public async Task InstallAsync_UriImage()
        {
            IProvider provider = _dependencies.GetProvider("filesystem");

            var desiredState = new LibraryInstallationState
            {
                ProviderId = "filesystem",
                Name = "http://glyphlist.azurewebsites.net/img/images/Flag.png",
                DestinationPath = "lib",
                Files = new[] { "Flag.png" }
            };

            ILibraryOperationResult result = await provider.InstallAsync(desiredState, CancellationToken.None);
            Assert.IsTrue(result.Success, "Didn't install");

            string copiedFile = Path.Combine(_projectFolder, desiredState.DestinationPath, desiredState.Files[0]);
            Assert.IsTrue(File.Exists(copiedFile), "File wasn't copied");
        }

        [TestMethod]
        public async Task InstallAsync_FileNotFound()
        {
            IProvider provider = _dependencies.GetProvider("filesystem");

            var desiredState = new LibraryInstallationState
            {
                ProviderId = "filesystem",
                Name = @"../file/does/not/exist.txt",
                DestinationPath = "lib",
                Files = new[] { "file.js" }
            };

            ILibraryOperationResult result = await provider.InstallAsync(desiredState, CancellationToken.None);
            Assert.IsFalse(result.Success);
            Assert.AreEqual("LIB002", result.Errors[0].Code);
        }

        [TestMethod]
        public async Task InstallAsync_PathNotDefined()
        {
            IProvider provider = _dependencies.GetProvider("filesystem");

            var desiredState = new LibraryInstallationState
            {
                ProviderId = "filesystem",
                Name = @"../file/does/not/exist.txt",
                Files = new[] { "file.js" }
            };

            ILibraryOperationResult result = await provider.InstallAsync(desiredState, CancellationToken.None);
            Assert.IsFalse(result.Success);
            Assert.AreEqual(result.Errors.Count(), 1);
            Assert.AreEqual("LIB002", result.Errors[0].Code);
        }

        [TestMethod]
        public async Task InstallAsync_IdNotDefined()
        {
            IProvider provider = _dependencies.GetProvider("filesystem");

            var desiredState = new LibraryInstallationState
            {
                ProviderId = "filesystem",
                DestinationPath = "lib",
                Files = new[] { "file.js" }
            };

            ILibraryOperationResult result = await provider.InstallAsync(desiredState, CancellationToken.None);
            Assert.IsFalse(result.Success);
            Assert.AreEqual("LIB002", result.Errors[0].Code);
        }

        [TestMethod]
        public async Task InstallAsync_ProviderNotDefined()
        {
            IProvider provider = _dependencies.GetProvider("filesystem");
            var desiredState = new LibraryInstallationState
            {
                Name = "http://glyphlist.azurewebsites.net/img/images/Flag.png",
                DestinationPath = "lib",
                Files = new[] { "Flag.png" }
            };

            ILibraryOperationResult result = await provider.InstallAsync(desiredState, CancellationToken.None);
            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public async Task RestoreAsync_Manifest()
        {
            IProvider provider = _dependencies.GetProvider("filesystem");
            string config = GetConfig();
            var manifest = Manifest.FromJson(config, _dependencies);
            IEnumerable<ILibraryOperationResult> result = await manifest.RestoreAsync(CancellationToken.None);

            Assert.IsTrue(result.Count() == 2, "Didn't install");

            string installed1 = Path.Combine(_projectFolder, "lib", "file1.txt");
            string installed2 = Path.Combine(_projectFolder, "lib", "file2.txt");
            Assert.IsTrue(File.Exists(installed1), "File1 wasn't copied");
            Assert.IsTrue(File.Exists(installed2), "File2 wasn't copied");
            Assert.AreEqual(File.ReadAllText(installed1), "test content");
            Assert.AreEqual(File.ReadAllText(installed2), "test content");
        }

        [TestMethod]
        public void GetSuggestedDestination()
        {
            IProvider provider = _dependencies.GetProvider("filesystem");
            Assert.AreEqual(string.Empty, provider.GetSuggestedDestination(null));

            var library = new FileSystemLibrary()
            {
                Name = "D:\\jquery\\",
                Files = null
            };

            Assert.AreEqual("jquery", provider.GetSuggestedDestination(library));

            library.Name = @"D:\jquery\jquery.min.js";

            Assert.AreEqual("jquery.min", provider.GetSuggestedDestination(library));
        }

        [TestMethod]
        private void GetCatalog()
        {
            IProvider provider = _dependencies.GetProvider("cdnjs");
            ILibraryCatalog catalog = provider.GetCatalog();

            Assert.IsNotNull(catalog);
        }

        private string GetConfig()
        {
            string config = $@"{{
  ""{ManifestConstants.Version}"": ""1.0"",
  ""{ManifestConstants.Libraries}"": [
    {{
      ""{ManifestConstants.Provider}"": ""filesystem"",
      ""{ManifestConstants.Library}"": ""_file1"",
      ""{ManifestConstants.Destination}"": ""lib"",
      ""{ManifestConstants.Files}"": [ ""file1.txt"" ]
    }},
    {{
      ""{ManifestConstants.Provider}"": ""filesystem"",
      ""{ManifestConstants.Library}"": ""_file2"",
      ""{ManifestConstants.Destination}"": ""lib"",
      ""{ManifestConstants.Files}"": [ ""file2.txt"" ]
    }}
  ]
}}
";

            return config.Replace("_file1", _file1).Replace("_file2", _file2).Replace("\\", "\\\\");
        }
    }
}
