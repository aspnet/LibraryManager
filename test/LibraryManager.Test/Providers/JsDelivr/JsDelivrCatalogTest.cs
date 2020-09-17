// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Cache;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Contracts.Caching;
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
        private readonly List<string> _prepopulatedFiles = new List<string>();

        private JsDelivrCatalog SetupCatalog(IWebRequestHandler webRequestHandler = null,
                                             INpmPackageSearch packageSearch = null,
                                             INpmPackageInfoFactory infoFactory = null,
                                             ICacheService cacheService = null,
                                             Dictionary<string, string> prepopulateFiles = null)
        {
            IWebRequestHandler defaultWebRequestHandler = webRequestHandler ?? new Mock<IWebRequestHandler>().Object;
            string cacheFolder = Environment.ExpandEnvironmentVariables(@"%localappdata%\Microsoft\Library\");

            if (prepopulateFiles != null)
            {
                foreach (KeyValuePair<string, string> item in prepopulateFiles)
                {
                    // put the provider IdText into the path to mimic the provider implementation
                    string filePath = Path.Combine(cacheFolder, JsDelivrProvider.IdText, item.Key);
                    string directoryPath = Path.GetDirectoryName(filePath);
                    Directory.CreateDirectory(directoryPath);
                    File.WriteAllText(filePath, item.Value);
                    _prepopulatedFiles.Add(filePath);
                }
            }

            return new JsDelivrCatalog(JsDelivrProvider.IdText,
                                       new VersionedLibraryNamingScheme(),
                                       new Mocks.Logger(),
                                       infoFactory ?? new NpmPackageInfoFactory(defaultWebRequestHandler),
                                       packageSearch ?? new NpmPackageSearch(defaultWebRequestHandler),
                                       cacheService ?? new CacheService(defaultWebRequestHandler),
                                       Path.Combine(cacheFolder, JsDelivrProvider.IdText));
        }

        [TestCleanup]
        public void CleanupPrepopulatedFiles()
        {
            foreach (string file in _prepopulatedFiles)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
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
            var fakeCache = new Mock<ICacheService>();
            fakeCache.SetupLibraryFiles("fakeLibrary@3.3.1", "fake/fakeLibrary@3.3.1");
            JsDelivrCatalog sut = SetupCatalog(cacheService: fakeCache.Object);

            CancellationToken token = CancellationToken.None;
            ILibrary library = await sut.GetLibraryAsync("fakeLibrary", "3.3.1", token);

            Assert.IsNotNull(library);
            Assert.AreEqual("fakeLibrary", library.Name);
            Assert.AreEqual("3.3.1", library.Version);

            ILibrary libraryGH = await sut.GetLibraryAsync("fake/fakeLibrary", "3.3.1", token);

            Assert.IsNotNull(libraryGH);
            Assert.AreEqual("fake/fakeLibrary", libraryGH.Name);
            Assert.AreEqual("3.3.1", libraryGH.Version);
        }

        [TestMethod]
        public async Task GetLibraryAsync_InvalidLibraryId()
        {
            JsDelivrCatalog sut = SetupCatalog();

            await Assert.ThrowsExceptionAsync<InvalidLibraryException>(() => sut.GetLibraryAsync("invalid_id", "", CancellationToken.None));
        }

        [TestMethod]
        public async Task GetLibraryAsync_CacheRequestFails_ShouldThrow()
        {
            var fakeCacheService = new Mock<ICacheService>();
            fakeCacheService.SetupBlockRequests();
            JsDelivrCatalog sut = SetupCatalog(cacheService: fakeCacheService.Object);

            await Assert.ThrowsExceptionAsync<InvalidLibraryException>(async () => await sut.GetLibraryAsync("fakeLibrary", "1.1.1", CancellationToken.None));
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
        public async Task GetLibraryCompletionSetAsync_Versions_Npm_ResultsFromCache()
        {
            var fakeNpmPackageInfoFactory = new Mock<INpmPackageInfoFactory>();
            fakeNpmPackageInfoFactory.Setup(x => x.GetPackageInfoAsync("fakeLib", It.IsAny<CancellationToken>()))
                                     .Returns(Task.FromResult(new NpmPackageInfo("fakeLib", "Fake library", "1.0.0", new[] { SemanticVersion.Parse("1.0.0"), SemanticVersion.Parse("2.0.0-beta") })));
            JsDelivrCatalog sut = SetupCatalog(infoFactory: fakeNpmPackageInfoFactory.Object);

            CompletionSet result = await sut.GetLibraryCompletionSetAsync("fakeLib@", 8);

            Assert.AreEqual(8, result.Start);
            Assert.AreEqual(0, result.Length);
            Assert.IsTrue(result.Completions.Count() > 0);
            CollectionAssert.AreEquivalent(new[] { "1.0.0", "2.0.0-beta", "latest" },
                                           result.Completions.Select(x => x.DisplayText).ToList());
        }

        [TestMethod]
        public async Task GetLibraryCompletionsSetAsync_Versions_GitHub_ResultsFromCache()
        {
            var cacheService = new Mock<ICacheService>();
            cacheService.SetupGitHubLibraryVersions("fake/fakeLib");
            JsDelivrCatalog sut = SetupCatalog(cacheService: cacheService.Object);

            string fakeLibraryId = "fake/fakeLib@abcdef";
            CompletionSet result = await sut.GetLibraryCompletionSetAsync(fakeLibraryId, fakeLibraryId.IndexOf('c'));

            Assert.IsNotNull(result);
            CollectionAssert.AreEquivalent(new[] { "0.1.2", "1.0.0-oldBeta", "1.2.3", "2.0.0-prerelease", "latest" },
                                           result.Completions.Select(x => x.DisplayText).ToList());
        }

        [TestMethod]
        public async Task GetLibraryCompletionsSetAsync_Versions_GitHub_CacheRequestFails_ShouldReturnEmptyList()
        {
            var cacheService = new Mock<ICacheService>();
            cacheService.SetupBlockRequests();
            JsDelivrCatalog sut = SetupCatalog(cacheService: cacheService.Object);

            string fakeLibName = "fake/fakeLib@abcdef";
            CompletionSet result = await sut.GetLibraryCompletionSetAsync(fakeLibName, fakeLibName.IndexOf('c'));

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Completions.Any());
        }

        [TestMethod]
        public async Task GetLatestVersion_LatestExist()
        {
            const string libraryId = "fakeLib@3.3.0";
            var fakeCache = new Mock<ICacheService>();
            fakeCache.SetupNpmLibraryVersions("fakeLib");
            JsDelivrCatalog sut = SetupCatalog(cacheService: fakeCache.Object);

            string result = await sut.GetLatestVersion(libraryId, false, CancellationToken.None);

            Assert.AreEqual("1.0.0", result);
        }

        [TestMethod]
        public async Task GetLatestVersion_GitHub_ParseVersionsList()
        {
            var fakeCache = new Mock<ICacheService>();
            fakeCache.SetupGitHubLibraryVersions("fake/fakeLib");
            JsDelivrCatalog sut = SetupCatalog(cacheService: fakeCache.Object);
            const string libraryIdGH = "fake/fakeLib@3.3.0";

            string resultGH = await sut.GetLatestVersion(libraryIdGH, false, CancellationToken.None);

            Assert.AreEqual("1.2.3", resultGH);
        }

        [TestMethod]
        public async Task GetLatestVersion_Npm_Prerelease()
        {
            const string libraryId = "fakeLib@3.3.0";
            var fakeCache = new Mock<ICacheService>();
            fakeCache.SetupNpmLibraryVersions("fakeLib");
            JsDelivrCatalog sut = SetupCatalog(cacheService: fakeCache.Object);

            string result = await sut.GetLatestVersion(libraryId, true, CancellationToken.None);

            Assert.AreEqual("2.0.0-beta2", result);
        }

        [TestMethod]
        public async Task GetLatestVersion_Git_Prerelease()
        {
            const string libraryId = "fakeLib/fakeLib@3.3.0";
            var fakeCache = new Mock<ICacheService>();
            fakeCache.SetupGitHubLibraryVersions("fakeLib/fakeLib");
            JsDelivrCatalog sut = SetupCatalog(cacheService: fakeCache.Object);

            string result = await sut.GetLatestVersion(libraryId, true, CancellationToken.None);

            Assert.AreEqual("2.0.0-prerelease", result);
        }


        [TestMethod]
        public async Task GetLatestVersion_CacheRequestFails_ReturnsNull()
        {
            var fakeCacheService = new Mock<ICacheService>();
            fakeCacheService.SetupBlockRequests();
            JsDelivrCatalog sut = SetupCatalog(cacheService: fakeCacheService.Object);

            string result = await sut.GetLatestVersion("fakeLibrary", false, CancellationToken.None);

            Assert.IsNull(result);
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
        public static Mock<ICacheService> SetupBlockRequests(this Mock<ICacheService> cacheService)
        {
            cacheService.Setup(x => x.GetContentsFromCachedFileWithWebRequestFallbackAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .Throws(new ResourceDownloadException("Cache requests blocked for testing"));
            cacheService.Setup(x => x.GetContentsFromUriWithCacheFallbackAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .Throws(new ResourceDownloadException("Cache requests blocked for testing"));

            return cacheService;
        }

        public static Mock<ICacheService> SetupNpmLibraryVersions(this Mock<ICacheService> cacheService, string libraryId)
        {
            string requestUrl = string.Format(JsDelivrCatalog.LatestLibraryVersionUrl, libraryId);
            cacheService.Setup(x => x.GetContentsFromUriWithCacheFallbackAsync(requestUrl, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.FromResult(FakeNpmVersions));

            return cacheService;
        }

        public static Mock<ICacheService> SetupGitHubLibraryVersions(this Mock<ICacheService> cacheService, string libraryId)
        {
            string requestUrl = string.Format(JsDelivrCatalog.LatestLibraryVersionUrlGH, libraryId);
            cacheService.Setup(x => x.GetContentsFromUriWithCacheFallbackAsync(requestUrl, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.FromResult(FakeGitHubVersions));

            return cacheService;
        }

        public static Mock<ICacheService> SetupLibraryFiles(this Mock<ICacheService> cacheService, string libraryId, string githubLibraryId)
        {
            string npmUrl = string.Format(JsDelivrCatalog.LibraryFileListUrlFormat, libraryId);
            string githubUrl = string.Format(JsDelivrCatalog.LibraryFileListUrlFormatGH, githubLibraryId);
            cacheService.Setup(x => x.GetContentsFromCachedFileWithWebRequestFallbackAsync(It.IsAny<string>(),
                                                                                           It.Is<string>(s => s.Equals(npmUrl, StringComparison.OrdinalIgnoreCase)),
                                                                                           It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(FakeFileList));
            cacheService.Setup(x => x.GetContentsFromCachedFileWithWebRequestFallbackAsync(It.IsAny<string>(),
                                                                                           It.Is<string>(s => s.Equals(githubUrl, StringComparison.OrdinalIgnoreCase)),
                                                                                           It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(FakeFileList));

            return cacheService;
        }

        public const string FakeFileList = @"{ ""files"": [ { ""name"": ""testFile.js"" } ] }";
        public const string FakeGitHubVersions = @"{ ""versions"": [ ""0.1.2"", ""1.0.0-oldBeta"", ""1.2.3"", ""2.0.0-prerelease"" ] }";
        public const string FakeNpmVersions = @"{
  ""tags"": {
    ""latest"": ""1.0.0""
  },
  versions: [
    ""2.0.0-beta"",
    ""2.0.0-beta2"",
    ""1.1.0""
  ]
}";
    }
}
