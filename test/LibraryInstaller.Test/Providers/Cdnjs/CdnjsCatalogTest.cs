// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryInstaller.Contracts;
using Microsoft.Web.LibraryInstaller.Mocks;
using Microsoft.Web.LibraryInstaller.Providers.Cdnjs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryInstaller.Test.Providers.Cdnjs
{
    [TestClass]
    public class CdnjsCatalogTest
    {
        private ILibraryCatalog _catalog;
        private IProvider _provider;

        [TestInitialize]
        public void Setup()
        {
            string projectFolder = Path.Combine(Path.GetTempPath(), "LibraryInstaller");
            string cacheFolder = Environment.ExpandEnvironmentVariables(@"%localappdata%\Microsoft\Library\");
            var hostInteraction = new HostInteraction(projectFolder, cacheFolder);
            var dependencies = new Dependencies(hostInteraction, new CdnjsProvider());

            _provider = dependencies.GetProvider("cdnjs");
            _catalog = _provider.GetCatalog();
        }

        [TestMethod]
        public async Task SearchAsync()
        {
            await SearchAsync(_provider, _catalog, @"jquery");
            await SearchAsync(_provider, _catalog, @"bootstrap");
            await SearchAsync(_provider, _catalog, @"knockout");
        }

        [TestMethod]
        public async Task SearchNoHitsAsync()
        {
            CancellationToken token = CancellationToken.None;

            IReadOnlyList<ILibraryGroup> absolute = await _catalog.SearchAsync(@"*9)_-", 1, token);
            Assert.AreEqual(0, absolute.Count);
        }

        private static async Task SearchAsync(IProvider provider, ILibraryCatalog catalog, string searchTerm)
        {
            CancellationToken token = CancellationToken.None;

            IReadOnlyList<ILibraryGroup> absolute = await catalog.SearchAsync(searchTerm, 1, token);
            Assert.AreEqual(1, absolute.Count);
            IEnumerable<string> libraryId = await absolute[0].GetLibraryIdsAsync(token);
            Assert.IsTrue(libraryId.Count() > 0);

            ILibrary library = await catalog.GetLibraryAsync(libraryId.First(), token);
            Assert.IsTrue(library.Files.Count > 0);
            Assert.AreEqual(1, library.Files.Count(f => f.Value));
            Assert.IsNotNull(library.Name);
            Assert.IsNotNull(library.Version);
            Assert.AreEqual(provider.Id, library.ProviderId);
        }

        [TestMethod]
        public async Task GetLibraryAsync()
        {
            CancellationToken token = CancellationToken.None;
            ILibrary library = await _catalog.GetLibraryAsync("jquery@3.1.1", token);

            Assert.IsNotNull(library);
            Assert.AreEqual("jquery", library.Name);
            Assert.AreEqual("3.1.1", library.Version);
        }

        [TestMethod]
        public async Task GetCompletionNameAsync()
        {
            CancellationToken token = CancellationToken.None;
            CompletionSet result = await _catalog.GetLibraryCompletionSetAsync("jquery", 0);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(6, result.Length);
            Assert.IsTrue(result.Completions.Count() > 300);
            Assert.AreEqual("jquery", result.Completions.First().DisplayText);
            Assert.IsTrue(result.Completions.First().InsertionText.StartsWith("jquery@"));
            Assert.IsTrue(result.Completions.First().InsertionText.Length >= 10);
        }

        [TestMethod]
        public async Task GetCompletionVersionAsync()
        {
            CancellationToken token = CancellationToken.None;
            CompletionSet result = await _catalog.GetLibraryCompletionSetAsync("jquery@", 7);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(7, result.Length);
            Assert.IsTrue(result.Completions.Count() >= 69);
            Assert.AreEqual("1.2.3", result.Completions.Last().DisplayText);
            Assert.AreEqual("jquery@1.2.3", result.Completions.Last().InsertionText);
        }

        [TestMethod]
        public async Task GetLatestVersionAsync()
        {
            CancellationToken token = CancellationToken.None;
            string libraryId = "twitter-bootstrap@3.3.0";
            string result = await _catalog.GetLatestVersion(libraryId, false, token);

            Assert.IsNotNull(result);

            string[] latest = result.Split('@');
            string[] existing = libraryId.Split('@');

            Assert.AreEqual(2, latest.Length);

            Assert.AreNotEqual(libraryId, result);
            Assert.AreEqual(existing[0], latest[0]);
        }
    }
}
