// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;
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
        private UnpkgCatalog SetupCatalog(IWebRequestHandler requestHandler = null, INpmPackageSearch packageSearch = null, INpmPackageInfoFactory infoFactory = null)
        {
            requestHandler = requestHandler ?? new Mocks.WebRequestHandler();
            return new UnpkgCatalog(UnpkgProvider.IdText,
                                    new VersionedLibraryNamingScheme(),
                                    new Logger(),
                                    requestHandler,
                                    infoFactory ?? new NpmPackageInfoFactory(requestHandler),
                                    packageSearch ?? new NpmPackageSearch(requestHandler));
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
            Mocks.WebRequestHandler handler = new Mocks.WebRequestHandler().SetupFiles("fakeLib@1.0.0");
            UnpkgCatalog sut = SetupCatalog(handler);

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
            var mockRequestHandler = new Mock<IWebRequestHandler>();
            UnpkgCatalog sut = SetupCatalog(mockRequestHandler.Object);

            CompletionSet result = await sut.GetLibraryCompletionSetAsync(null, 0);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(0, result.Length);
            Assert.AreEqual(0, result.Completions.Count());
            mockRequestHandler.Verify(m => m.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_EmptyString()
        {
            var mockRequestHandler = new Mock<IWebRequestHandler>();
            UnpkgCatalog sut = SetupCatalog(mockRequestHandler.Object);

            CompletionSet result = await sut.GetLibraryCompletionSetAsync(string.Empty, 0);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(0, result.Length);
            Assert.AreEqual(0, result.Completions.Count());
            mockRequestHandler.Verify(m => m.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
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
            Mocks.WebRequestHandler handler = new Mocks.WebRequestHandler().SetupVersions("fakeLibrary");
            UnpkgCatalog sut = SetupCatalog(handler);

            string result = await sut.GetLatestVersion(libraryName, false, CancellationToken.None);

            Assert.AreEqual("1.0.0", result);
        }

        [TestMethod]
        [Ignore] // TODO: GetLatestVersion currently doesn't distinguish stable and pre-release versions
        public async Task GetLatestVersion_PreRelease()
        {
            const string libraryName = "fakeLibrary";
            Mocks.WebRequestHandler handler = new Mocks.WebRequestHandler().SetupVersions("fakeLibrary");
            UnpkgCatalog sut = SetupCatalog(handler);

            string result = await sut.GetLatestVersion(libraryName, true, CancellationToken.None);

            Assert.AreEqual("2.0.0-beta", result);
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
        public static Mocks.WebRequestHandler SetupFiles(this Mocks.WebRequestHandler h, string libraryId)
        {
            string files = @"{
  ""type"": ""directory"",
  ""files"": [
    {
      ""path"": ""testFile.js"",
      ""type"": ""file""
    }
  ]
}";

            (string name, string version) = new VersionedLibraryNamingScheme().GetLibraryNameAndVersion(libraryId);

            return h.ArrangeResponse(string.Format(UnpkgCatalog.LibraryFileListUrlFormat, name, version), files);
        }

        public static Mocks.WebRequestHandler SetupVersions(this Mocks.WebRequestHandler h, string libraryName)
        {
            string packageData = @"{
  ""version"": ""1.0.0""
}";

            return h.ArrangeResponse(string.Format(UnpkgCatalog.LatestLibraryVersonUrl, libraryName), packageData);
        }
    }

}
