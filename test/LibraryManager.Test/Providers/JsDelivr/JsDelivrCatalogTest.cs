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
using Microsoft.Web.LibraryManager.Providers.Unpkg;
using Moq;
using static Microsoft.Web.LibraryManager.Test.TestUtilities.StringUtility;

namespace Microsoft.Web.LibraryManager.Test.Providers.JsDelivr
{
    [TestClass]
    public class JsDelivrCatalogTest
    {
        private static JsDelivrCatalog SetupCatalog(IWebRequestHandler webRequestHandler = null, INpmPackageSearch packageSearch = null, INpmPackageInfoFactory infoFactory = null)
        {
            webRequestHandler = webRequestHandler ?? new Mocks.WebRequestHandler();
            return new JsDelivrCatalog(JsDelivrProvider.IdText,
                                       new VersionedLibraryNamingScheme(),
                                       new Mocks.Logger(),
                                       webRequestHandler,
                                       infoFactory ?? new NpmPackageInfoFactory(webRequestHandler),
                                       packageSearch ?? new NpmPackageSearch(webRequestHandler));
        }

        [TestMethod]
        public async Task SearchAsync_Success()
        {
            string searchTerm = "jquery";
            var mockSearch = new Mock<INpmPackageSearch>();
            NpmPackageInfo[] expectedResult = new[] { new NpmPackageInfo("fakepackage", "", "1.0.0") };
            mockSearch.Setup(m => m.GetPackageNamesAsync("jquery", It.IsAny<CancellationToken>()))
                      .Returns(Task.FromResult(expectedResult.AsEnumerable()));
            JsDelivrCatalog sut = SetupCatalog(packageSearch: mockSearch.Object);

            IReadOnlyList<ILibraryGroup> result = await sut.SearchAsync(searchTerm, 1, CancellationToken.None);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("fakepackage", result[0].DisplayName);
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
        public async Task GetLibraryCompletionSetAsync_ScopedPackageNameisSingleAt_ReturnsNoCompletions_MakesNoWebRequest()
        {
            var mockRequestHandler = new Mock<IWebRequestHandler>();
            JsDelivrCatalog sut = SetupCatalog(mockRequestHandler.Object);
            (string nameStart, int caretPos) = ExtractCaret("@|");

            CompletionSet result = await sut.GetLibraryCompletionSetAsync(nameStart, caretPos);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(0, result.Completions.Count());
            mockRequestHandler.Verify(m => m.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_Names()
        {
            var mockSearch = new Mock<INpmPackageSearch>();
            mockSearch.Setup(m => m.GetPackageNamesAsync("jquery", It.IsAny<CancellationToken>()))
                      .Returns(Task.FromResult(new[] { new NpmPackageInfo("fakePackage1", "", "1.0.0") }.AsEnumerable()));
            JsDelivrCatalog sut = SetupCatalog(packageSearch: mockSearch.Object);
            (string nameStart, int caretPos) = ExtractCaret("|jquery");

            CompletionSet result = await sut.GetLibraryCompletionSetAsync(nameStart, caretPos);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(6, result.Length);
            CollectionAssert.AreEquivalent(new[] { "fakePackage1" },
                                           result.Completions.Select(c => c.DisplayText).ToList());
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_ScopesNoName()
        {
            var mockSearch = new Mock<INpmPackageSearch>();
            mockSearch.Setup(m => m.GetPackageNamesAsync("@testscope", It.IsAny<CancellationToken>()))
                      .Returns(Task.FromResult(new[] { new NpmPackageInfo("fakePackage1", "", "1.0.0") }.AsEnumerable()));
            JsDelivrCatalog sut = SetupCatalog(packageSearch: mockSearch.Object);
            (string nameStart, int caretPos) = ExtractCaret("|@testscope");

            CompletionSet result = await sut.GetLibraryCompletionSetAsync(nameStart, caretPos);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(10, result.Length);
            CollectionAssert.AreEquivalent(new[] { "fakePackage1" },
                                           result.Completions.Select(c => c.DisplayText).ToList());
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_ScopesWithName()
        {
            var mockSearch = new Mock<INpmPackageSearch>();
            mockSearch.Setup(m => m.GetPackageNamesAsync("@testscope/package", It.IsAny<CancellationToken>()))
                      .Returns(Task.FromResult(new[] { new NpmPackageInfo("fakePackage1", "", "1.0.0") }.AsEnumerable()));
            JsDelivrCatalog sut = SetupCatalog(packageSearch: mockSearch.Object);
            (string nameStart, int caretPos) = ExtractCaret("|@testscope/package");

            CompletionSet result = await sut.GetLibraryCompletionSetAsync(nameStart, caretPos);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(18, result.Length);
            CollectionAssert.AreEquivalent(new[] { "fakePackage1" },
                                           result.Completions.Select(c => c.DisplayText).ToList());
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_ScopesWithNameAndTrailingAt_CursorAtVersions()
        {
            var mockPackageInfoFactory = new Mock<INpmPackageInfoFactory>();
            mockPackageInfoFactory.Setup(m => m.GetPackageInfoAsync("@types/react", It.IsAny<CancellationToken>()))
                                  .Returns(Task.FromResult(new NpmPackageInfo("fakepackage", "", "2.0.0", new List<SemanticVersion> { SemanticVersion.Parse("1.0.0"), SemanticVersion.Parse("2.0.0") })));
            JsDelivrCatalog sut = SetupCatalog(infoFactory: mockPackageInfoFactory.Object);
            (string nameStart, int caretPos) = ExtractCaret("@types/react@|");

            CompletionSet result = await sut.GetLibraryCompletionSetAsync(nameStart, caretPos);

            Assert.AreEqual(13, result.Start);
            Assert.AreEqual(0, result.Length);
            Assert.AreEqual(CompletionSortOrder.Version, result.CompletionType);
            CollectionAssert.AreEqual(new[] { "2.0.0", "1.0.0", "latest" },
                                      result.Completions.Select(c => c.DisplayText).ToList());
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_ScopesWithNameAndTrailingAt_CursorAtName()
        {
            var mockSearch = new Mock<INpmPackageSearch>();
            // TODO: we should strip the trailing @ from the name
            mockSearch.Setup(m => m.GetPackageNamesAsync("@types/react@", It.IsAny<CancellationToken>()))
                      .Returns(Task.FromResult(new[] { new NpmPackageInfo("fakePackage1", "", "1.0.0") }.AsEnumerable()));
            JsDelivrCatalog sut = SetupCatalog(packageSearch: mockSearch.Object);
            (string nameStart, int caretPos) = ExtractCaret("@types/r|eact@");

            CompletionSet result = await sut.GetLibraryCompletionSetAsync(nameStart, caretPos);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(13, result.Length);
            CollectionAssert.AreEquivalent(new[] { "fakePackage1" },
                                           result.Completions.Select(c => c.DisplayText).ToList());
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_ScopesWithNameAndVersions_CursorInVersionsSubstring()
        {
            var mockPackageInfoFactory = new Mock<INpmPackageInfoFactory>();
            mockPackageInfoFactory.Setup(m => m.GetPackageInfoAsync("@types/react", It.IsAny<CancellationToken>()))
                                  .Returns(Task.FromResult(new NpmPackageInfo("fakepackage", "", "2.0.0", new List<SemanticVersion> { SemanticVersion.Parse("1.0.0"), SemanticVersion.Parse("2.0.0") })));
            JsDelivrCatalog sut = SetupCatalog(infoFactory: mockPackageInfoFactory.Object);
            (string nameStart, int caretPos) = ExtractCaret("@types/react@1|");

            CompletionSet result = await sut.GetLibraryCompletionSetAsync(nameStart, caretPos);

            Assert.AreEqual(13, result.Start);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(CompletionSortOrder.Version, result.CompletionType);
            CollectionAssert.AreEqual(new[] { "2.0.0", "1.0.0", "latest" },
                                      result.Completions.Select(c => c.DisplayText).ToList());
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_ScopesWithNameAndVersions_CursorInNameSubstring()
        {
            var mockSearch = new Mock<INpmPackageSearch>();
            // TODO: do we really not strip the version out here?  Seems like we should...
            mockSearch.Setup(m => m.GetPackageNamesAsync("@types/node@1.0.0", It.IsAny<CancellationToken>()))
                      .Returns(Task.FromResult(new[] { new NpmPackageInfo("fakePackage1", "", "1.0.0") }.AsEnumerable()));
            JsDelivrCatalog sut = SetupCatalog(packageSearch: mockSearch.Object);
            (string nameStart, int caretPos) = ExtractCaret("@types/no|de@1.0.0");

            CompletionSet result = await sut.GetLibraryCompletionSetAsync(nameStart, caretPos);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(17, result.Length);
            CollectionAssert.AreEquivalent(new[] { "fakePackage1" },
                                           result.Completions.Select(c => c.DisplayText).ToList());
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_LibraryNameWithLeadingAndTrailingWhitespace_WhitespaceIncludedInSearchTerm()
        {
            var mockSearch = new Mock<INpmPackageSearch>();
            mockSearch.Setup(m => m.GetPackageNamesAsync("    jquery ", It.IsAny<CancellationToken>()))
                      .Returns(Task.FromResult(new[] { new NpmPackageInfo("fakePackage1", "", "1.0.0") }.AsEnumerable()));
            JsDelivrCatalog sut = SetupCatalog(packageSearch: mockSearch.Object);

            CompletionSet result = await sut.GetLibraryCompletionSetAsync("    jquery ", 0);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(11, result.Length);
            CollectionAssert.AreEquivalent(new[] { "fakePackage1" },
                                           result.Completions.Select(c => c.DisplayText).ToList());
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_NullValue_MakesNoWebRequest()
        {
            var mockRequestHandler = new Mock<IWebRequestHandler>();
            JsDelivrCatalog sut = SetupCatalog(mockRequestHandler.Object);

            CompletionSet result = await sut.GetLibraryCompletionSetAsync(null, 0);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(0, result.Length);
            Assert.AreEqual(0, result.Completions.Count());
            mockRequestHandler.Verify(m => m.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_EmptyString_MakesNoWebRequest()
        {
            var mockRequestHandler = new Mock<IWebRequestHandler>();
            JsDelivrCatalog sut = SetupCatalog(mockRequestHandler.Object);

            CompletionSet result = await sut.GetLibraryCompletionSetAsync(string.Empty, 0);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(0, result.Length);
            Assert.AreEqual(0, result.Completions.Count());
            mockRequestHandler.Verify(m => m.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
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

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_ReturnsCompletionWithLatestVersion()
        {
            //Arrange
            var packageSearch = new Mock<INpmPackageSearch>();
            var infoFactory = new Mock<INpmPackageInfoFactory>();
            var testPkgInfo = new NpmPackageInfo(name: "testPkg", description: "description", latestVersion: "1.2.3");

            var packages = new List<NpmPackageInfo>() { testPkgInfo };
            packageSearch.Setup(p => p.GetPackageNamesAsync(It.Is<string>(s => string.Equals(s, "testPkg")), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult((IEnumerable<NpmPackageInfo>)packages));

            infoFactory.Setup(p => p.GetPackageInfoAsync(It.Is<string>(s => string.Equals(s, "testPkg")), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(testPkgInfo));


            JsDelivrCatalog sut = SetupCatalog(packageSearch: packageSearch.Object, infoFactory: infoFactory.Object);

            //Act
            CompletionSet result = await sut.GetLibraryCompletionSetAsync("testPkg", 7);

            //Assert
            Assert.AreEqual(1, result.Completions.Count());
            Assert.AreEqual("testPkg", result.Completions.First().DisplayText);
            Assert.AreEqual("testPkg@1.2.3", result.Completions.First().InsertionText);
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
