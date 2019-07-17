// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.LibraryNaming;
using Microsoft.Web.LibraryManager.Providers.jsDelivr;

namespace Microsoft.Web.LibraryManager.Test.Providers.JsDelivr
{
    [TestClass]
    public class JsDelivrCatalogTest
    {
        private static JsDelivrCatalog SetupCatalog(IWebRequestHandler webRequestHandler = null)
        {
            return new JsDelivrCatalog(JsDelivrProvider.IdText,
                                       new VersionedLibraryNamingScheme(),
                                       new Mocks.Logger(),
                                       webRequestHandler ?? new Mocks.WebRequestHandler());
        }

        [TestMethod]
        public async Task SearchAsync_Success()
        {
            string searchTerm = "jquery";
            JsDelivrCatalog sut = SetupCatalog();

            IReadOnlyList<ILibraryGroup> absolute = await sut.SearchAsync(searchTerm, 1, CancellationToken.None);
            Assert.AreEqual(100, absolute.Count);
            IEnumerable<string> libraryVersions = await absolute[0].GetLibraryVersions(CancellationToken.None);
            CollectionAssert.Contains(libraryVersions.ToList(), "3.4.1");
        }

        [TestMethod]
        public async Task SearchAsync_NoHits()
        {
            // The search service is surprisingly flexible for finding full-text matches, so this
            // gibberish string was determined manually.
            string searchTerm = "*9(_-zv_";
            JsDelivrCatalog sut = SetupCatalog();

            IReadOnlyList<ILibraryGroup> absolute = await sut.SearchAsync(searchTerm, 1, CancellationToken.None);

            Assert.AreEqual(0, absolute.Count);
        }

        [TestMethod]
        public async Task SearchAsync_EmptyString()
        {
            JsDelivrCatalog sut = SetupCatalog();

            IReadOnlyList<ILibraryGroup> absolute = await sut.SearchAsync("", 1, CancellationToken.None);

            Assert.AreEqual(0, absolute.Count);
        }

        [TestMethod]
        public async Task SearchAsync_NullString()
        {
            JsDelivrCatalog sut = SetupCatalog();

            IReadOnlyList<ILibraryGroup> absolute = await sut.SearchAsync(null, 1, CancellationToken.None);

            Assert.AreEqual(0, absolute.Count);
        }

        [TestMethod]
        public async Task GetLibraryAsync_Success()
        {
            Mocks.WebRequestHandler fakeRequestHandler = new Mocks.WebRequestHandler().SetupFiles("jquery@3.3.1", "jquery/jquery@3.3.1");
            JsDelivrCatalog sut = SetupCatalog(fakeRequestHandler);

            CancellationToken token = CancellationToken.None;
            ILibrary library = await sut.GetLibraryAsync("jquery", "3.3.1", token);

            Assert.IsNotNull(library);
            Assert.AreEqual("jquery", library.Name);
            Assert.AreEqual("3.3.1", library.Version);

            ILibrary libraryGH = await sut.GetLibraryAsync("jquery/jquery", "3.3.1", token);

            Assert.IsNotNull(libraryGH);
            Assert.AreEqual("jquery/jquery", libraryGH.Name);
            Assert.AreEqual("3.3.1", libraryGH.Version);
        }

        [TestMethod]
        public async Task GetLibraryAsync_InvalidLibraryId()
        {
            JsDelivrCatalog sut = SetupCatalog();

            await Assert.ThrowsExceptionAsync<InvalidLibraryException>(() => sut.GetLibraryAsync("invalid_id", "", CancellationToken.None));
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_ScopedPackageNameisSingleAt_ReturnsNoCompletions()
        {
            JsDelivrCatalog sut = SetupCatalog();

            CompletionSet result = await sut.GetLibraryCompletionSetAsync("@", 1);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(0, result.Completions.Count());
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_Names()
        {
            JsDelivrCatalog sut = SetupCatalog();

            CompletionSet result = await sut.GetLibraryCompletionSetAsync("jquery", 0);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(6, result.Length);
            Assert.AreEqual(100, result.Completions.Count());
            Assert.AreEqual("jquery", result.Completions.First().DisplayText);
            Assert.IsTrue(result.Completions.First().InsertionText.StartsWith("jquery"));
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_ScopesNoName()
        {
            JsDelivrCatalog sut = SetupCatalog();

            CompletionSet result = await sut.GetLibraryCompletionSetAsync("@types/", 0);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(7, result.Length);
            Assert.AreEqual(25, result.Completions.Count());
            Assert.AreEqual("@types/node", result.Completions.First().DisplayText);
            Assert.IsTrue(result.Completions.First().InsertionText.StartsWith("@types/node"));
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_ScopesWithName()
        {
            JsDelivrCatalog sut = SetupCatalog();

            CompletionSet result = await sut.GetLibraryCompletionSetAsync("@types/node", 0);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(11, result.Length);
            Assert.AreEqual(25, result.Completions.Count());
            Assert.AreEqual("@types/node", result.Completions.First().DisplayText);
            Assert.IsTrue(result.Completions.First().InsertionText.StartsWith("@types/node"));
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_ScopesWithNameAndTrailingAt_CursorAtVersions()
        {
            JsDelivrCatalog sut = SetupCatalog();

            CompletionSet result = await sut.GetLibraryCompletionSetAsync("@types/react@", 13);

            Assert.AreEqual(13, result.Start);
            Assert.AreEqual(0, result.Length);
            Assert.IsTrue(result.Completions.Count() > 0);
            Assert.IsTrue(result.Completions.First().InsertionText.StartsWith("@types/react"));
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_ScopesWithNameAndTrailingAt_CursorAtName()
        {
            JsDelivrCatalog sut = SetupCatalog();

            CompletionSet result = await sut.GetLibraryCompletionSetAsync("@types/react@", 6);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(13, result.Length);
            Assert.IsTrue(result.Completions.Count() > 0);
            Assert.IsTrue(result.Completions.First().InsertionText.StartsWith("@types/react"));
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_ScopesWithNameAndVersions_CursorInVersionsSubstring()
        {
            JsDelivrCatalog sut = SetupCatalog();

            CompletionSet result = await sut.GetLibraryCompletionSetAsync("@types/react@1", 14);

            Assert.AreEqual(13, result.Start);
            Assert.AreEqual(1, result.Length);
            Assert.IsTrue(result.Completions.Count() > 0);
            Assert.IsTrue(result.Completions.First().InsertionText.StartsWith("@types/react"));
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_ScopesWithNameAndVersions_CursorInNameSubstring()
        {
            JsDelivrCatalog sut = SetupCatalog();

            CompletionSet result = await sut.GetLibraryCompletionSetAsync("@types/node@1.0.2", 8);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(17, result.Length);
            Assert.AreEqual(1, result.Completions.Count());
            Assert.IsTrue(result.Completions.First().InsertionText.StartsWith("@types/node"));
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_LibraryNameWithLeadingAndTrailingWhitespace()
        {
            JsDelivrCatalog sut = SetupCatalog();

            CancellationToken token = CancellationToken.None;
            CompletionSet result = await sut.GetLibraryCompletionSetAsync("    jquery ", 0);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(11, result.Length);
            Assert.AreEqual(100, result.Completions.Count());
            Assert.AreEqual("jquery", result.Completions.First().DisplayText);
            Assert.IsTrue(result.Completions.First().InsertionText.StartsWith("jquery"));
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_NullValue()
        {
            JsDelivrCatalog sut = SetupCatalog();

            CancellationToken token = CancellationToken.None;
            CompletionSet result = await sut.GetLibraryCompletionSetAsync(null, 0);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(0, result.Length);
            Assert.AreEqual(0, result.Completions.Count());
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_EmptyString()
        {
            JsDelivrCatalog sut = SetupCatalog();

            CancellationToken token = CancellationToken.None;
            CompletionSet result = await sut.GetLibraryCompletionSetAsync(string.Empty, 0);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(0, result.Length);
            Assert.AreEqual(0, result.Completions.Count());
        }

        [TestMethod]
        [Ignore] // Enable it after version completion sorting is committed.
                 // TODO: Also add a test for GitHub version completion
        public async Task GetLibraryCompletionSetAsync_Versions()
        {
            JsDelivrCatalog sut = SetupCatalog();

            CompletionSet result = await sut.GetLibraryCompletionSetAsync("jquery@", 7);

            Assert.AreEqual(7, result.Start);
            Assert.AreEqual(0, result.Length);
            Assert.IsTrue(result.Completions.Count() > 0);
            Assert.AreEqual("1.5.1", result.Completions.Last().DisplayText);
            Assert.AreEqual("jquery@1.5.1", result.Completions.Last().InsertionText);
        }

        [TestMethod]
        public async Task GetLatestVersion_LatestExist()
        {
            const string libraryId = "fakeLib@3.3.0";
            Mocks.WebRequestHandler fakeHandler = new Mocks.WebRequestHandler().SetupVersions("fakeLib", "fake/fakeLib");
            JsDelivrCatalog sut = SetupCatalog(fakeHandler);

            string result = await sut.GetLatestVersion(libraryId, false, CancellationToken.None);

            Assert.AreEqual("1.0.0", result);

            // TODO: A new test should be added for when the tags are missing.  This is passing because the fake
            //       data is as expected.  However, real data does not include the tags and would return null.
            //       The original test implementation checked for null and didn't assert anything in that case.
            const string libraryIdGH = "fake/fakeLib@3.3.0";
            string resultGH = await sut.GetLatestVersion(libraryIdGH, false, CancellationToken.None);

            Assert.AreEqual("1.0.0", resultGH);
        }

        [TestMethod]
        [Ignore] // TODO: GetLatestVersion currently only looks for the stable tag.
        public async Task GetLatestVersion_PreRelease()
        {
            const string libraryId = "fakeLib@3.3.0";
            Mocks.WebRequestHandler fakeHandler = new Mocks.WebRequestHandler().SetupVersions("fakeLib", "fake/fakeLib");
            JsDelivrCatalog sut = SetupCatalog(fakeHandler);

            string result = await sut.GetLatestVersion(libraryId, true, CancellationToken.None);

            Assert.AreEqual("2.0.0-beta", result);
        }
    }

    internal static class JsDelivrCatalogSetups
    {
        public static Mocks.WebRequestHandler SetupFiles(this Mocks.WebRequestHandler h, string libraryId, string githubLibraryId)
        {
            string files = @"{ ""files"": [ { ""name"": ""testFile.js"" } ] }";

            return h.ArrangeResponse(string.Format(JsDelivrCatalog.LibraryFileListUrlFormat, libraryId), files)
                    .ArrangeResponse(string.Format(JsDelivrCatalog.LibraryFileListUrlFormatGH, githubLibraryId), files);
        }

        public static Mocks.WebRequestHandler SetupVersions(this Mocks.WebRequestHandler h, string libraryId, string githubLibraryId)
        {
            string versions = @"{
  ""tags"": {
    ""beta"": ""2.0.0-beta"",
    ""latest"": ""1.0.0""
  }
}";

            return h.ArrangeResponse(string.Format(JsDelivrCatalog.LatestLibraryVersionUrl, libraryId), versions)
                    .ArrangeResponse(string.Format(JsDelivrCatalog.LatestLibraryVersionUrlGH, githubLibraryId), versions);
        }
    }
}
