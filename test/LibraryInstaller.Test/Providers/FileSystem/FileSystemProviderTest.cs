using LibraryInstaller.Contracts;
using LibraryInstaller.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using LibraryInstaller.Providers.FileSystem;
using System;
using System.Linq;

namespace LibraryInstaller.Test.Providers.FileSystem
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
            _projectFolder = Path.Combine(Path.GetTempPath(), "LibraryInstaller\\");
            _configFilePath = Path.Combine(_projectFolder, "library.json");
            _relativeSrc = Path.Combine(_projectFolder, "folder", "file.txt");

            var hostInteraction = new HostInteraction(_projectFolder, "");
            _dependencies = new Dependencies(hostInteraction, new FileSystemProvider());

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
            Directory.Delete(_projectFolder, true);
        }

        [TestMethod]
        public async Task InstallAsync()
        {
            IProvider provider = _dependencies.GetProvider("filesystem");

            var desiredState = new LibraryInstallationState
            {
                ProviderId = "filesystem",
                LibraryId = _file1,
                Path = "lib",
                Files = new[] { "file1.txt" }
            };

            ILibraryInstallationResult result = await provider.InstallAsync(desiredState, CancellationToken.None);
            Assert.IsTrue(result.Success, "Didn't install");

            string copiedFile = Path.Combine(_projectFolder, desiredState.Path, desiredState.Files[0]);
            Assert.IsTrue(File.Exists(copiedFile), "File1 wasn't copied");

            var manifest = Manifest.FromJson("{}", _dependencies);
            manifest.AddLibrary(desiredState);
            await manifest.SaveAsync(_configFilePath, CancellationToken.None);

            Assert.IsTrue(File.Exists(_configFilePath));
            Assert.AreEqual(File.ReadAllText(copiedFile), "test content");
        }

        [TestMethod]
        public async Task InstallRelativeFileAsync()
        {
            IProvider provider = _dependencies.GetProvider("filesystem");

            var desiredState = new LibraryInstallationState
            {
                ProviderId = "filesystem",
                LibraryId = "folder/file.txt",
                Path = "lib",
                Files = new[] { "relative.txt" }
            };

            ILibraryInstallationResult result = await provider.InstallAsync(desiredState, CancellationToken.None);
            Assert.IsTrue(result.Success, "Didn't install");

            string copiedFile = Path.Combine(_projectFolder, desiredState.Path, desiredState.Files[0]);
            Assert.IsTrue(File.Exists(copiedFile), "File1 wasn't copied");
            Assert.IsFalse(result.Cancelled);
            Assert.AreSame(desiredState, result.InstallationState);
            Assert.AreEqual(0, result.Errors.Count);

            var manifest = Manifest.FromJson("{}", _dependencies);
            manifest.AddLibrary(desiredState);
            await manifest.SaveAsync(_configFilePath, CancellationToken.None);

            Assert.IsTrue(File.Exists(_configFilePath));
            Assert.AreEqual(File.ReadAllText(copiedFile), "test content");
        }

        [TestMethod]
        public async Task InstallFolderFilesAsync()
        {
            string folder = Path.Combine(Path.GetTempPath(), "LibraryInstaller_test");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "file1.js"), "");
            File.WriteAllText(Path.Combine(folder, "file2.js"), "");
            File.WriteAllText(Path.Combine(folder, "file3.js"), "");

            IProvider provider = _dependencies.GetProvider("filesystem");

            var desiredState = new LibraryInstallationState
            {
                ProviderId = "filesystem",
                LibraryId = folder,
                Path = "lib",
                Files = new[] { "file1.js", "file2.js" }
            };

            ILibraryInstallationResult result = await provider.InstallAsync(desiredState, CancellationToken.None);
            Assert.IsTrue(result.Success, "Didn't install");

            string file1 = Path.Combine(_projectFolder, desiredState.Path, desiredState.Files[0]);
            string file2 = Path.Combine(_projectFolder, desiredState.Path, desiredState.Files[1]);
            Assert.IsTrue(File.Exists(file1), "File1 wasn't copied");
            Assert.IsTrue(File.Exists(file2), "File2 wasn't copied");

            Assert.IsFalse(result.Cancelled);
            Assert.AreSame(desiredState, result.InstallationState);
            Assert.AreEqual(0, result.Errors.Count);
        }

        [TestMethod]
        public async Task InstallRelativeFolderFilesAsync()
        {
            string folder = Path.Combine(Path.GetTempPath(), "LibraryInstaller_test\\");
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
                LibraryId = relativeFolder.OriginalString,
                Path = "lib",
                Files = new[] { "file1.js", "file2.js" }
            };

            ILibraryInstallationResult result = await provider.InstallAsync(desiredState, CancellationToken.None);
            Assert.IsTrue(result.Success, "Didn't install");

            string file1 = Path.Combine(_projectFolder, desiredState.Path, desiredState.Files[0]);
            string file2 = Path.Combine(_projectFolder, desiredState.Path, desiredState.Files[1]);
            Assert.IsTrue(File.Exists(file1), "File1 wasn't copied");
            Assert.IsTrue(File.Exists(file2), "File1 wasn't copied");

            Assert.IsFalse(result.Cancelled);
            Assert.AreSame(desiredState, result.InstallationState);
            Assert.AreEqual(0, result.Errors.Count);
        }

        [TestMethod]
        public async Task InstallUriAsync()
        {
            IProvider provider = _dependencies.GetProvider("filesystem");

            var desiredState = new LibraryInstallationState
            {
                ProviderId = "filesystem",
                LibraryId = "https://raw.githubusercontent.com/jquery/jquery/master/src/event.js",
                Path = "lib",
                Files = new[] { "event.js" }
            };

            ILibraryInstallationResult result = await provider.InstallAsync(desiredState, CancellationToken.None);
            Assert.IsTrue(result.Success, "Didn't install");

            string copiedFile = Path.Combine(_projectFolder, desiredState.Path, desiredState.Files[0]);
            Assert.IsTrue(File.Exists(copiedFile), "File wasn't copied");

            var manifest = Manifest.FromJson("{}", _dependencies);
            manifest.AddLibrary(desiredState);
            await manifest.SaveAsync(_configFilePath, CancellationToken.None);

            Assert.IsTrue(File.Exists(_configFilePath));
            Assert.IsTrue(File.ReadAllText(copiedFile).Length > 1000);
        }

        [TestMethod]
        public async Task InstallUriImageAsync()
        {
            IProvider provider = _dependencies.GetProvider("filesystem");

            var desiredState = new LibraryInstallationState
            {
                ProviderId = "filesystem",
                LibraryId = "http://glyphlist.azurewebsites.net/img/images/Flag.png",
                Path = "lib",
                Files = new[] { "Flag.png" }
            };

            ILibraryInstallationResult result = await provider.InstallAsync(desiredState, CancellationToken.None);
            Assert.IsTrue(result.Success, "Didn't install");

            string copiedFile = Path.Combine(_projectFolder, desiredState.Path, desiredState.Files[0]);
            Assert.IsTrue(File.Exists(copiedFile), "File wasn't copied");
        }

        [TestMethod]
        public async Task RestoreFromManifestAsync()
        {
            IProvider provider = _dependencies.GetProvider("filesystem");
            string config = GetConfig();
            var manifest = Manifest.FromJson(config, _dependencies);
            IEnumerable<ILibraryInstallationResult> result = await manifest.RestoreAsync(CancellationToken.None);

            Assert.IsTrue(result.Count() == 2, "Didn't install");

            string installed1 = Path.Combine(_projectFolder, "lib", "file1.txt");
            string installed2 = Path.Combine(_projectFolder, "lib", "file2.txt");
            Assert.IsTrue(File.Exists(installed1), "File1 wasn't copied");
            Assert.IsTrue(File.Exists(installed2), "File2 wasn't copied");
            Assert.AreEqual(File.ReadAllText(installed1), "test content");
            Assert.AreEqual(File.ReadAllText(installed2), "test content");
        }

        [TestMethod]
        public async Task InstallFileNotFoundAsync()
        {
            IProvider provider = _dependencies.GetProvider("filesystem");

            var desiredState = new LibraryInstallationState
            {
                ProviderId = "filesystem",
                LibraryId = @"../file/does/not/exist.txt",
                Path = "lib",
                Files = new[] { "file.js" }
            };

            ILibraryInstallationResult result = await provider.InstallAsync(desiredState, CancellationToken.None);
            Assert.IsFalse(result.Success);
            Assert.AreEqual("LIB002", result.Errors[0].Code);
        }

        private string GetConfig()
        {
            string config = @"{
  ""version"": ""1.0"",
  ""packages"": [
    {
      ""provider"": ""filesystem"",
      ""id"": ""_file1"",
      ""path"": ""lib"",
      ""files"": [ ""file1.txt"" ]
    },
    {
      ""provider"": ""filesystem"",
      ""id"": ""_file2"",
      ""path"": ""lib"",
      ""files"": [ ""file2.txt"" ]
    }
  ]
}
";

            return config.Replace("_file1", _file1).Replace("_file2", _file2).Replace("\\", "\\\\");
        }
    }
}