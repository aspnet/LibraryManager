// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Mocks;
using Microsoft.Web.LibraryManager.Providers.FileSystem;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Test.Providers.FileSystem
{
    [TestClass]
    public class FileSystemCatalogTest
    {
        private string _projectFolder;
        private IProvider _provider;
        private ILibraryCatalog _catalog;

        [TestInitialize]
        public void Setup()
        {
            _projectFolder = Path.Combine(Path.GetTempPath(), "LibraryManager");

            var hostInteraction = new HostInteraction(_projectFolder, "");
            var dependencies = new Dependencies(hostInteraction, new FileSystemProviderFactory());
            _provider = dependencies.GetProvider("filesystem");
            _catalog = new FileSystemCatalog((FileSystemProvider) _provider, true);
        }

        [DataTestMethod]
        [DataRow(@"c:\file.txt")]
        [DataRow(@"../path/to/file.txt")]
        [DataRow(@"file.txt")]
        [DataRow(@"http://example.com/file.txt")]
        public async Task SearchAsync_File(string file)
        {
            CancellationToken token = CancellationToken.None;

            IReadOnlyList<ILibraryGroup> absolute = await _catalog.SearchAsync(file, 1, token);
            Assert.AreEqual(1, absolute.Count);
            IEnumerable<string> info = await absolute[0].GetLibraryIdsAsync(token);
            Assert.AreEqual(1, info.Count());

            ILibrary library = await _catalog.GetLibraryAsync(info.First(), token);
            Assert.AreEqual(1, library.Files.Count);
            Assert.AreEqual(1, library.Files.Count(f => f.Value));
            Assert.AreEqual(file, library.Name);
            Assert.AreEqual("1.0", library.Version);
            Assert.AreEqual(_provider.Id, library.ProviderId);
            Assert.AreEqual(Path.GetFileName(file), library.Files.Keys.ElementAt(0));
        }

        [TestMethod]
        public async Task SearchAsync_NoHits()
        {
            CancellationToken token = CancellationToken.None;
            IReadOnlyList<ILibraryGroup> absolute = await _catalog.SearchAsync("*9)_-|\"?:", 1, token);
            Assert.AreEqual(1, absolute.Count);
        }

        [TestMethod]
        public async Task SearchAsync_Folder()
        {
            string folder = Path.Combine(Path.GetTempPath(), "LibraryManager_test");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "file1.js"), "");
            File.WriteAllText(Path.Combine(folder, "file2.js"), "");
            File.WriteAllText(Path.Combine(folder, "file3.js"), "");

            CancellationToken token = CancellationToken.None;

            IReadOnlyList<ILibraryGroup> absolute = await _catalog.SearchAsync(folder, 3, token);
            Assert.AreEqual(1, absolute.Count);

            IEnumerable<string> info = await absolute.First().GetLibraryIdsAsync(token);
            Assert.AreEqual(1, info.Count());

            ILibrary library = await _catalog.GetLibraryAsync(info.First(), token);
            Assert.AreEqual(3, library.Files.Count);

            Directory.Delete(folder, true);
        }

        [TestMethod]
        public async Task SearchAsync_EmptyString()
        {
            CancellationToken token = CancellationToken.None;
            IReadOnlyList<ILibraryGroup> absolute = await _catalog.SearchAsync("", 1, token);
            Assert.AreEqual(1, absolute.Count);
        }

        [TestMethod]
        public async Task SearchAsync_NullString()
        {
            CancellationToken token = CancellationToken.None;
            IReadOnlyList<ILibraryGroup> absolute = await _catalog.SearchAsync(null, 1, token);
            Assert.AreEqual(1, absolute.Count);
        }


        [TestMethod]
        public async Task GetLibraryAsync_File()
        {
            ILibrary library = await _catalog.GetLibraryAsync(@"c:\some\path\to\file.js", CancellationToken.None);
            Assert.IsNotNull(library);
            Assert.AreEqual(1, library.Files.Count);
            Assert.AreEqual("file.js", library.Files.ElementAt(0).Key);
        }

        [TestMethod]
        public async Task GetLibraryAsync_Folder()
        {
            string folder = Path.Combine(Path.GetTempPath(), "LibraryManager_test");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "file1.js"), "");
            File.WriteAllText(Path.Combine(folder, "file2.js"), "");
            File.WriteAllText(Path.Combine(folder, "file3.js"), "");

            ILibrary library = await _catalog.GetLibraryAsync(folder, CancellationToken.None);
            Assert.IsNotNull(library);
            Assert.AreEqual(3, library.Files.Count);
            Assert.AreEqual("file1.js", library.Files.ElementAt(0).Key);
        }

        [TestMethod]
        public async Task GetLibraryAsync_Uri()
        {
            ILibrary library = await _catalog.GetLibraryAsync("http://example.com/file.js", CancellationToken.None);
            Assert.IsNotNull(library);
            Assert.AreEqual(1, library.Files.Count);
            Assert.AreEqual("file.js", library.Files.ElementAt(0).Key);
        }

        [DataTestMethod]
        [DataRow("http://foo.com/file.txt")]
        [DataRow("https://foo.com/file.txt")]
        [DataRow("ftp://foo.com/file.txt")]
        [DataRow("file://foo.com/file.txt")]
        public async Task GetLibraryCompletionSetAsync_Uri(string url)
        {
            CompletionSet result = await _catalog.GetLibraryCompletionSetAsync(url, 0);

            Assert.AreEqual(default(CompletionSet), result);
            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(0, result.Length);
            Assert.IsNull(result.Completions);
        }

        [DataTestMethod]
        [DataRow("test/file.txt", 5, 2)]
        [DataRow("test/file.txt", 4, 1)]
        [DataRow("test/file.txt", 4, 1)]
        public async Task GetLibraryCompletionSetAsync_RelativePath(string path, int caretPos, int completions)
        {
            Directory.CreateDirectory(Path.Combine(_projectFolder, "test"));
            File.WriteAllText(Path.Combine(_projectFolder, "test", "file1.txt"), "");
            File.WriteAllText(Path.Combine(_projectFolder, "test", "file2.txt"), "");

            CompletionSet result = await _catalog.GetLibraryCompletionSetAsync(path, caretPos);

            Assert.AreEqual(completions, result.Completions.Count());
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_AbsolutePath()
        {
            DirectoryInfo dir = Directory.CreateDirectory(Path.Combine(_projectFolder, "test2\\"));
            File.WriteAllText(Path.Combine(dir.FullName, "file1.txt"), "");
            File.WriteAllText(Path.Combine(dir.FullName, "file2.txt"), "");
            File.WriteAllText(Path.Combine(dir.FullName, "file3.txt"), "");

            CompletionSet result = await _catalog.GetLibraryCompletionSetAsync(dir.FullName, dir.FullName.Length);

            Assert.AreEqual(3, result.Completions.Count());
        }

        [TestMethod]
        public void GetLatestVersion()
        {
            CancellationToken token = CancellationToken.None;
            string libraryId = "myfile.js";
            Task<string> result = _catalog.GetLatestVersion(libraryId, false, token);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsCompleted);
            Assert.AreEqual(string.Empty, result.Result);

        }
    }
}
