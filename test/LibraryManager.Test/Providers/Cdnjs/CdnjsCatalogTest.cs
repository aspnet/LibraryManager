// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Mocks;
using Microsoft.Web.LibraryManager.Providers.Cdnjs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Test.Providers.Cdnjs
{
    [TestClass]
    public class CdnjsCatalogTest
    {
        private ILibraryCatalog _catalog;
        private IProvider _provider;

        [TestInitialize]
        public void Setup()
        {
            string projectFolder = Path.Combine(Path.GetTempPath(), "LibraryManager");
            string cacheFolder = Environment.ExpandEnvironmentVariables(@"%localappdata%\Microsoft\Library\");
            var hostInteraction = new HostInteraction(projectFolder, cacheFolder);
            var dependencies = new Dependencies(hostInteraction, new CdnjsProviderFactory());

            _provider = dependencies.GetProvider("cdnjs");
            _catalog = _provider.GetCatalog();
        }

        [DataTestMethod]
        [DataRow("jquery", "jquery")]
        [DataRow("knockout", "knockout")]
        [DataRow("backbone", "backbone.js")]
        public async Task SearchAsync_Success(string searchTerm, string expectedId)
        {
            CancellationToken token = CancellationToken.None;

            IReadOnlyList<ILibraryGroup> absolute = await _catalog.SearchAsync(searchTerm, 1, token);
            Assert.AreEqual(1, absolute.Count);
            IEnumerable<string> libraryId = await absolute[0].GetLibraryIdsAsync(token);
            Assert.IsTrue(libraryId.Any());

            ILibrary library = await _catalog.GetLibraryAsync(libraryId.First(), token);
            Assert.IsTrue(library.Files.Count > 0);
            Assert.AreEqual(expectedId, library.Name);
            Assert.AreEqual(1, library.Files.Count(f => f.Value));
            Assert.IsNotNull(library.Name);
            Assert.IsNotNull(library.Version);
            Assert.AreEqual(_provider.Id, library.ProviderId);
        }

        [TestMethod]
        public async Task SearchAsync_NoHits()
        {
            CancellationToken token = CancellationToken.None;

            IReadOnlyList<ILibraryGroup> absolute = await _catalog.SearchAsync(@"*9)_-", 1, token);
            Assert.AreEqual(0, absolute.Count);
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
        public async Task GetLibraryAsync_Success()
        {
            CancellationToken token = CancellationToken.None;
            ILibrary library = await _catalog.GetLibraryAsync("jquery@3.1.1", token);

            Assert.IsNotNull(library);
            Assert.AreEqual("jquery", library.Name);
            Assert.AreEqual("3.1.1", library.Version);
        }

        [TestMethod, ExpectedException(typeof(InvalidLibraryException))]
        public async Task GetLibraryAsync_InvalidLibraryId()
        {
            CancellationToken token = CancellationToken.None;
            ILibrary library = await _catalog.GetLibraryAsync("invalid_id", token);
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_Names()
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
        public async Task GetLibraryCompletionSetAsync_Versions()
        {
            CancellationToken token = CancellationToken.None;
            CompletionSet result = await _catalog.GetLibraryCompletionSetAsync("jquery@", 7);

            Assert.AreEqual(7, result.Start);
            Assert.AreEqual(0, result.Length);
            Assert.IsTrue(result.Completions.Count() >= 69);
            Assert.AreEqual("1.2.3", result.Completions.Last().DisplayText);
            Assert.AreEqual("jquery@1.2.3", result.Completions.Last().InsertionText);
        }

        [TestMethod]
        public async Task GetLatestVersion_LatestExist()
        {
            CancellationToken token = CancellationToken.None;
            const string libraryId = "twitter-bootstrap@3.3.0";
            string result = await _catalog.GetLatestVersion(libraryId, false, token);

            Assert.IsNotNull(result);

            string[] existing = libraryId.Split('@');

            Assert.AreNotEqual(existing[1], result);
        }

        [TestMethod]
        public async Task GetLatestVersion_PreRelease()
        {
            CancellationToken token = CancellationToken.None;
            const string libraryId = "twitter-bootstrap@3.3.0";
            string result = await _catalog.GetLatestVersion(libraryId, true, token);

            Assert.IsNotNull(result);

            string[] existing = libraryId.Split('@');

            Assert.AreNotEqual(existing[1], result);
        }
    }
}
