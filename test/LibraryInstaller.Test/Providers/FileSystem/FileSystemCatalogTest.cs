// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using LibraryInstaller.Contracts;
using LibraryInstaller.Mocks;
using LibraryInstaller.Providers.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryInstaller.Test.Providers.FileSystem
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
            _projectFolder = Path.Combine(Path.GetTempPath(), "LibraryInstaller");

            var hostInteraction = new HostInteraction(_projectFolder, "");
            var dependencies = new Dependencies(hostInteraction, new FileSystemProvider());
            _provider = dependencies.GetProvider("filesystem");
            _catalog = _provider.GetCatalog();
        }

        [TestMethod]
        public async Task SearchAsync()
        {
            await SearchAsync(_provider, _catalog, @"c:\file.txt");
            await SearchAsync(_provider, _catalog, @"../path/to/file.txt");
            await SearchAsync(_provider, _catalog, @"file.txt");
            await SearchAsync(_provider, _catalog, @"http://example.com/file.txt");
        }

        [TestMethod]
        public async Task SearchNoHitsAsync()
        {
            CancellationToken token = CancellationToken.None;
            IReadOnlyList<ILibraryGroup> absolute = await _catalog.SearchAsync("*9)_-|\"?:", 1, token);
            Assert.AreEqual(1, absolute.Count);
        }

        [TestMethod]
        public async Task SearchFolderAsync()
        {
            string folder = Path.Combine(Path.GetTempPath(), "LibraryInstaller_test");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "file1.js"), "");
            File.WriteAllText(Path.Combine(folder, "file2.js"), "");
            File.WriteAllText(Path.Combine(folder, "file3.js"), "");

            CancellationToken token = CancellationToken.None;

            IReadOnlyList<ILibraryGroup> absolute = await _catalog.SearchAsync(folder, 3, token);
            Assert.AreEqual(1, absolute.Count);

            IReadOnlyList<ILibraryDisplayInfo> info = await absolute.First().GetDisplayInfosAsync(token);
            Assert.AreEqual(1, info.Count);

            ILibrary library = await _catalog.GetLibraryAsync(info.First().LibraryId, token);
            Assert.AreEqual(3, library.Files.Count);

            Directory.Delete(folder, true);
        }

        private static async Task SearchAsync(IProvider provider, ILibraryCatalog catalog, string file)
        {
            CancellationToken token = CancellationToken.None;

            IReadOnlyList<ILibraryGroup> absolute = await catalog.SearchAsync(file, 1, token);
            Assert.AreEqual(1, absolute.Count);
            IReadOnlyList<ILibraryDisplayInfo> info = await absolute[0].GetDisplayInfosAsync(token);
            Assert.AreEqual(1, info.Count);

            ILibrary library = await catalog.GetLibraryAsync(info[0].LibraryId, token);
            Assert.AreEqual(1, library.Files.Count);
            Assert.AreEqual(1, library.Files.Count(f => f.Value));
            Assert.AreEqual(file, library.Name);
            Assert.AreEqual("1.0", library.Version);
            Assert.AreEqual(provider.Id, library.ProviderId);
            Assert.AreEqual(Path.GetFileName(file), library.Files.Keys.ElementAt(0));
        }

        [TestMethod]
        public async Task GetLibraryAsync()
        {
            ILibrary library = await _catalog.GetLibraryAsync(@"c:\some\path\to\file.js", CancellationToken.None);
            Assert.IsNotNull(library);
            Assert.AreEqual(1, library.Files.Count);
            Assert.AreEqual("file.js", library.Files.ElementAt(0).Key);
        }

        [TestMethod]
        public async Task GetCompletionNameAsync()
        {
            CompletionSet result = await _catalog.GetLibraryCompletionSetAsync("../file.txt", 0);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(0, result.Length);
            Assert.IsNull(result.Completions);
        }
    }
}
