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
using Microsoft.Web.LibraryManager.Mocks;
using Microsoft.Web.LibraryManager.Providers.Unpkg;
using Moq;
using static Microsoft.Web.LibraryManager.Test.TestUtilities.StringUtility;

namespace Microsoft.Web.LibraryManager.Test.Providers.Unpkg
{
    [TestClass]
    public class UnpkgCatalogTest
    {
        private readonly List<string> _prepopulatedFiles = new List<string>();

        private UnpkgCatalog SetupCatalog(ICacheService cacheService = null, INpmPackageSearch packageSearch = null, INpmPackageInfoFactory infoFactory = null, Dictionary<string, string> prepopulateFiles = null)
        {
            string cacheFolder = Environment.ExpandEnvironmentVariables(@"%localappdata%\Microsoft\Library\");
            if (prepopulateFiles != null)
            {
                foreach (KeyValuePair<string, string> item in prepopulateFiles)
                {
                    // put the provider IdText into the path to mimic the provider implementation
                    string filePath = Path.Combine(cacheFolder, UnpkgProvider.IdText, item.Key);
                    string directoryPath = Path.GetDirectoryName(filePath);
                    Directory.CreateDirectory(directoryPath);
                    File.WriteAllText(filePath, item.Value);
                    _prepopulatedFiles.Add(filePath);
                }
            }

            IWebRequestHandler defaultRequestHandler = new Mocks.WebRequestHandler();
            return new UnpkgCatalog(UnpkgProvider.IdText,
                                    new VersionedLibraryNamingScheme(),
                                    new Logger(),
                                    infoFactory ?? new NpmPackageInfoFactory(defaultRequestHandler),
                                    packageSearch ?? new NpmPackageSearch(defaultRequestHandler),
                                    cacheService ?? new CacheService(defaultRequestHandler),
                                    Path.Combine(cacheFolder, UnpkgProvider.IdText));
        }

        [TestCleanup]
        public void CleanupPrepopulatedFiles()
        {
            foreach (string file in _prepopulatedFiles)
            {
                File.Delete(file);
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
            UnpkgCatalog sut = SetupCatalog(packageSearch: mockSearch.Object);

            IReadOnlyList<ILibraryGroup> result = await sut.SearchAsync(searchTerm, 1, CancellationToken.None);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("fakepackage", result[0].DisplayName);
        }

        [TestMethod]
        public async Task GetLibraryAsync_Success()
        {
            var fakeCache = new Mock<ICacheService>();
            fakeCache.SetupLibraryFiles("fakeLib");
            UnpkgCatalog sut = SetupCatalog(fakeCache.Object);

            ILibrary library = await sut.GetLibraryAsync("fakeLib", "1.0.0", CancellationToken.None);

            Assert.IsNotNull(library);
            Assert.AreEqual("fakeLib", library.Name);
            Assert.AreEqual("1.0.0", library.Version);
        }

        [TestMethod]
        public async Task GetLibraryAsync_InvalidLibraryId()
        {
            UnpkgCatalog sut = SetupCatalog();

            await Assert.ThrowsExceptionAsync<InvalidLibraryException>(async () => await sut.GetLibraryAsync("invalid_id", "invalid_version", CancellationToken.None));
        }

        [TestMethod]
        public async Task GetLibraryAsync_RequestFilesFailsWithNoCache_BlowsUp()
        {
            var webRequestHandler = new Mock<IWebRequestHandler>();
            webRequestHandler.Setup(x => x.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                             .Throws(new ResourceDownloadException("Cache download blocked."));
            var fakeCache = new Mock<ICacheService>();
            fakeCache.SetupBlockRequests();
            UnpkgCatalog sut = SetupCatalog(fakeCache.Object);

            await Assert.ThrowsExceptionAsync<InvalidLibraryException>(async () => await sut.GetLibraryAsync("fakeLibrary", "1.1.1", CancellationToken.None));
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_Names()
        {
            var mockSearch = new Mock<INpmPackageSearch>();
            mockSearch.Setup(m => m.GetPackageNamesAsync("jquery", It.IsAny<CancellationToken>()))
                      .Returns(Task.FromResult(new[] { new NpmPackageInfo("fakePackage1", "", "1.0.0") }.AsEnumerable()));
            UnpkgCatalog sut = SetupCatalog(packageSearch: mockSearch.Object);

            CompletionSet result = await sut.GetLibraryCompletionSetAsync("jquery", 0);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(6, result.Length);
            CollectionAssert.AreEquivalent(new[] { "fakePackage1" },
                                           result.Completions.Select(c => c.DisplayText).ToList());
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_ScopesNoName()
        {
            var mockSearch = new Mock<INpmPackageSearch>();
            mockSearch.Setup(m => m.GetPackageNamesAsync("@types/", It.IsAny<CancellationToken>()))
                      .Returns(Task.FromResult(new[] { new NpmPackageInfo("fakePackage1", "", "1.0.0") }.AsEnumerable()));
            UnpkgCatalog sut = SetupCatalog(packageSearch: mockSearch.Object) ;
            (string searchTerm, int caretPos) = ExtractCaret("|@types/");

            CompletionSet result = await sut.GetLibraryCompletionSetAsync(searchTerm, caretPos);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(7, result.Length);
            CollectionAssert.AreEquivalent(new[] { "fakePackage1" },
                                           result.Completions.Select(c => c.DisplayText).ToList());
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_ScopesWithName()
        {
            var mockSearch = new Mock<INpmPackageSearch>();
            mockSearch.Setup(m => m.GetPackageNamesAsync("@types/node", It.IsAny<CancellationToken>()))
                      .Returns(Task.FromResult(new[] { new NpmPackageInfo("fakePackage1", "", "1.0.0") }.AsEnumerable()));
            UnpkgCatalog sut = SetupCatalog(packageSearch: mockSearch.Object);
            (string searchTerm, int caretPos) = ExtractCaret("|@types/node");

            CompletionSet result = await sut.GetLibraryCompletionSetAsync(searchTerm, caretPos);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(11, result.Length);
            CollectionAssert.AreEquivalent(new[] { "fakePackage1" },
                                           result.Completions.Select(c => c.DisplayText).ToList());
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_ScopesWithNameAndTrailingAt_CursorAtVersions()
        {
            var mockPackageInfoFactory = new Mock<INpmPackageInfoFactory>();
            mockPackageInfoFactory.Setup(m => m.GetPackageInfoAsync("@types/react", It.IsAny<CancellationToken>()))
                                  .Returns(Task.FromResult(new NpmPackageInfo("fakepackage", "", "2.0.0", new List<SemanticVersion> { SemanticVersion.Parse("1.0.0"), SemanticVersion.Parse("2.0.0") })));
            UnpkgCatalog sut = SetupCatalog(infoFactory: mockPackageInfoFactory.Object);
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
            UnpkgCatalog sut = SetupCatalog(packageSearch: mockSearch.Object);
            (string nameStart, int caretPos) = ExtractCaret("@types/r|eact@");

            CompletionSet result = await sut.GetLibraryCompletionSetAsync(nameStart, caretPos);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(13, result.Length);
            Assert.AreEqual(CompletionSortOrder.AsSpecified, result.CompletionType);
            CollectionAssert.AreEquivalent(new[] { "fakePackage1" },
                                           result.Completions.Select(c => c.DisplayText).ToList());
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_ScopesWithNameAndVersions_CursorInNameSubstring()
        {
            var mockSearch = new Mock<INpmPackageSearch>();
            // TODO: do we really not strip the version out here?  Seems like we should...
            mockSearch.Setup(m => m.GetPackageNamesAsync("@types/node@1.0.0", It.IsAny<CancellationToken>()))
                      .Returns(Task.FromResult(new[] { new NpmPackageInfo("fakePackage1", "", "1.0.0") }.AsEnumerable()));
            UnpkgCatalog sut = SetupCatalog(packageSearch: mockSearch.Object);
            (string nameStart, int caretPos) = ExtractCaret("@types/no|de@1.0.0");

            CompletionSet result = await sut.GetLibraryCompletionSetAsync(nameStart, caretPos);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(17, result.Length);
            CollectionAssert.AreEquivalent(new[] { "fakePackage1" },
                                           result.Completions.Select(c => c.DisplayText).ToList());
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_LibraryNameWithLeadingAndTrailingWhitespace()
        {
            var mockSearch = new Mock<INpmPackageSearch>();
            mockSearch.Setup(m => m.GetPackageNamesAsync("    jquery ", It.IsAny<CancellationToken>()))
                      .Returns(Task.FromResult(new[] { new NpmPackageInfo("fakePackage1", "", "1.0.0") }.AsEnumerable()));
            UnpkgCatalog sut = SetupCatalog(packageSearch: mockSearch.Object);

            CancellationToken token = CancellationToken.None;
            CompletionSet result = await sut.GetLibraryCompletionSetAsync("    jquery ", 0);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(11, result.Length);
            CollectionAssert.AreEquivalent(new[] { "fakePackage1" },
                                           result.Completions.Select(c => c.DisplayText).ToList());
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_NullValue_MakesNoWebRequest()
        {
            var mockPackageSearch = new Mock<INpmPackageSearch>();
            var mockPackageInfo = new Mock<INpmPackageInfoFactory>();
            UnpkgCatalog sut = SetupCatalog(packageSearch: mockPackageSearch.Object, infoFactory: mockPackageInfo.Object);

            CompletionSet result = await sut.GetLibraryCompletionSetAsync(null, 0);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(0, result.Length);
            Assert.AreEqual(0, result.Completions.Count());
            mockPackageInfo.VerifyNoOtherCalls();
            mockPackageSearch.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_EmptyString_MakesNoWebRequest()
        {
            var mockPackageSearch = new Mock<INpmPackageSearch>();
            var mockPackageInfo = new Mock<INpmPackageInfoFactory>();
            UnpkgCatalog sut = SetupCatalog(packageSearch: mockPackageSearch.Object, infoFactory: mockPackageInfo.Object);

            CompletionSet result = await sut.GetLibraryCompletionSetAsync(string.Empty, 0);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(0, result.Length);
            Assert.AreEqual(0, result.Completions.Count());
            mockPackageInfo.VerifyNoOtherCalls();
            mockPackageSearch.VerifyNoOtherCalls();
        }


        [TestMethod]
        [Ignore] // Enable it after version completion sorting is committed.
        public async Task GetLibraryCompletionSetAsync_Versions()
        {
            UnpkgCatalog sut = SetupCatalog();

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
            const string libraryName = "fakeLibrary";
            var fakeCache = new Mock<ICacheService>();
            fakeCache.SetupPackageVersions(libraryName);
            UnpkgCatalog sut = SetupCatalog(fakeCache.Object);

            string result = await sut.GetLatestVersion(libraryName, false, CancellationToken.None);

            Assert.AreEqual("1.0.0", result);
        }

        [TestMethod]
        public async Task GetLatestVersion_Prerelease()
        {
            const string libraryName = "fakeLibrary";
            var versions = new List<SemanticVersion>()
            {
                SemanticVersion.Parse("2.0.0-beta"),
                SemanticVersion.Parse("1.0.0"),
            };
            var fakeCache = new Mock<ICacheService>();
            fakeCache.SetupPackageVersions(libraryName);
            var fakePackageInfoFactory = new Mock<INpmPackageInfoFactory>();
            fakePackageInfoFactory.Setup(f => f.GetPackageInfoAsync(It.Is<string>(s => s == libraryName), It.IsAny<CancellationToken>()))
                                  .Returns(Task.FromResult(new NpmPackageInfo(libraryName, "test package", "1.0.0", versions)));
            UnpkgCatalog sut = SetupCatalog(fakeCache.Object, infoFactory: fakePackageInfoFactory.Object);

            string result = await sut.GetLatestVersion(libraryName, includePreReleases: true, CancellationToken.None);

            Assert.AreEqual("2.0.0-beta", result);
        }

        [TestMethod]
        public async Task GetLatestVersion_WebResponseFailedButNoCachedFile_ReturnsNull()
        {
            var fakeCache = new Mock<ICacheService>();
            fakeCache.SetupBlockRequests();
            UnpkgCatalog sut = SetupCatalog(fakeCache.Object);

            string result = await sut.GetLatestVersion("fakeLibrary", false, CancellationToken.None);

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_ScopedPackageNameisSingleAt_ReturnsNoCompletions()
        {
            UnpkgCatalog sut = SetupCatalog();

            CompletionSet result = await sut.GetLibraryCompletionSetAsync("@", 1);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(0, result.Completions.Count());
            Assert.AreEqual(CompletionSortOrder.AsSpecified, result.CompletionType);
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

            UnpkgCatalog sut = SetupCatalog(packageSearch: packageSearch.Object, infoFactory: infoFactory.Object);

            //Act
            CompletionSet result = await sut.GetLibraryCompletionSetAsync("testPkg", 7);

            //Assert
            Assert.AreEqual(1, result.Completions.Count());
            Assert.AreEqual("testPkg", result.Completions.First().DisplayText);
            Assert.AreEqual("testPkg@1.2.3", result.Completions.First().InsertionText);
        }
    }

    internal static class UnpkgCatalogSetups
    {
        public static Mock<ICacheService> SetupLibraryFiles(this Mock<ICacheService> cacheService, string name)
        {
            cacheService.Setup(x => x.GetContentsFromCachedFileWithWebRequestFallbackAsync(It.Is<string>(s => s.Contains(name)),
                                                                                           It.IsAny<string>(),
                                                                                           It.IsAny<CancellationToken>()))
                        .Returns(Task.FromResult(FakeFileList));

            return cacheService;
        }

        public static Mock<ICacheService> SetupBlockRequests(this Mock<ICacheService> cacheService)
        {
            cacheService.Setup(x => x.GetContentsFromCachedFileWithWebRequestFallbackAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .Throws(new ResourceDownloadException("Cache requests blocked for testing"));
            cacheService.Setup(x => x.GetContentsFromUriWithCacheFallbackAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .Throws(new ResourceDownloadException("Cache requests blocked for testing"));

            return cacheService;
        }

        public static Mock<ICacheService> SetupPackageVersions(this Mock<ICacheService> cacheService, string name)
        {
            cacheService.Setup(x => x.GetContentsFromUriWithCacheFallbackAsync(It.Is<string>(s => s.Contains(name)),
                                                                               It.IsAny<string>(),
                                                                               It.IsAny<CancellationToken>()))
                        .Returns(Task.FromResult(FakePackageVersions));

            return cacheService;
        }

        public const string FakePackageVersions = @"{
  ""version"": ""1.0.0""
}";

        public const string FakeFileList = @"{
  ""type"": ""directory"",
  ""files"": [
    {
      ""path"": ""testFile.js"",
      ""type"": ""file""
    }
  ]
}";
    }

}
