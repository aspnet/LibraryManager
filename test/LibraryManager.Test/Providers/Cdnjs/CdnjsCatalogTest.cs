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
using Microsoft.Web.LibraryManager.Providers.Cdnjs;

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

            LibraryIdToNameAndVersionConverter.Instance.EnsureInitialized(dependencies);

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

        [TestMethod]
        public void ConvertToLibraryGroup_ValidJsonCatalog()
        {
            string json = @"{""results"":[{""name"":""1140"",""latest"":""https://cdnjs.cloudflare.com/ajax/libs/1140/2.0/1140.min.css"",
""description"":""The 1140 grid fits perfectly into a 1280 monitor. On smaller monitors it becomes fluid and adapts to the width of the browser.""
,""version"":""2.0""}],""total"":1}";

            CdnjsCatalog cdnjsCatalog = _catalog as CdnjsCatalog;

            IEnumerable<CdnjsLibraryGroup> libraryGroup = cdnjsCatalog.ConvertToLibraryGroups(json);

            Assert.AreEqual(1, libraryGroup.Count());
            CdnjsLibraryGroup library = libraryGroup.First();
            Assert.AreEqual("1140", library.DisplayName);
            Assert.AreEqual("The 1140 grid fits perfectly into a 1280 monitor. On smaller monitors it becomes fluid and adapts to the width of the browser.", library.Description);
            Assert.AreEqual("2.0", library.Version);
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(@"{""results"":[12}")]
        public void ConvertToLibraryGroup_InvalidJsonCatalog(string json)
        {
            CdnjsCatalog cdnjsCatalog = _catalog as CdnjsCatalog;

            IEnumerable<CdnjsLibraryGroup> libraryGroup = cdnjsCatalog.ConvertToLibraryGroups(json);

            Assert.IsNull(libraryGroup);
        }

        [TestMethod]
        public void ConvertToAssets_ValidAsset()
        {
            string json = @"{""name"":""jquery"",""filename"":""jquery.min.js"",""version"":""3.3.1"",""description"":""JavaScript library for DOM operations"",
""homepage"":""http://jquery.com/"",""keywords"":[""jquery"",""library"",""ajax"",""framework"",""toolkit"",""popular""],""namespace"":""jQuery"",
""repository"":{""type"":""git"",""url"":""https://github.com/jquery/jquery.git""},""license"":""MIT"",
""author"":{""name"":""jQuery Foundation and other contributors"",""url"":""https://github.com/jquery/jquery/blob/master/AUTHORS.txt""},
""autoupdate"":{""type"":""npm"",""target"":""jquery""},
""assets"":[{""version"":""3.3.1"",""files"":[""core.js"",""jquery.js"",""jquery.min.js"",""jquery.min.map"",""jquery.slim.js"",""jquery.slim.min.js"",""jquery.slim.min.map""]}]}";

            CdnjsCatalog cdnjsCatalog = _catalog as CdnjsCatalog;

            List<Asset> list = cdnjsCatalog.ConvertToAssets(json);

            Assert.AreEqual(1, list.Count());
            Asset asset = list[0];
            Assert.AreEqual("3.3.1", asset.Version);

            string[] expectedFiles = new string[] { "core.js", "jquery.js", "jquery.min.js", "jquery.min.map", "jquery.slim.js", "jquery.slim.min.js", "jquery.slim.min.map" };
            Assert.AreEqual(7, asset.Files.Count());
            foreach(string file in expectedFiles)
            {
                Assert.IsTrue(asset.Files.Contains(file));
            }

            Assert.AreEqual("jquery.min.js", asset.DefaultFile);
        }

        [TestMethod]
        public void ConvertToAssets_InvalidAsset()
        {
            string json = "abcd";

            CdnjsCatalog cdnjsCatalog = _catalog as CdnjsCatalog;

            List<Asset> list = cdnjsCatalog.ConvertToAssets(json);

            Assert.IsNull(list);
        }
    }
}
