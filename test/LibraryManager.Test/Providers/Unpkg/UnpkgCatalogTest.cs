// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.LibraryNaming;
using Microsoft.Web.LibraryManager.Providers.Unpkg;

namespace Microsoft.Web.LibraryManager.Test.Providers.Unpkg
{
    [TestClass]
    public class UnpkgCatalogTest
    {
        private UnpkgCatalog SetupCatalog(IWebRequestHandler handler = null)
        {
            return new UnpkgCatalog(UnpkgProvider.IdText,
                                    new VersionedLibraryNamingScheme(),
                                    new Mocks.Logger(),
                                    handler ?? new Mocks.WebRequestHandler());
        }

        [TestMethod]
        public async Task SearchAsync_Success()
        {
            string searchTerm = "jquery";
            UnpkgCatalog sut = SetupCatalog();

            IReadOnlyList<ILibraryGroup> absolute = await sut.SearchAsync(searchTerm, 1, CancellationToken.None);
            Assert.AreEqual(100, absolute.Count);
            IEnumerable<string> libraryVersions = await absolute[0].GetLibraryVersions(CancellationToken.None);
            Assert.IsTrue(libraryVersions.Any());
        }

        [TestMethod]
        public async Task SearchAsync_NoHits()
        {
            // The search service is surprisingly flexible for finding full-text matches, so this
            // gibberish string was determined manually.
            string searchTerm = "*9(_-zv_";
            UnpkgCatalog sut = SetupCatalog();

            IReadOnlyList<ILibraryGroup> absolute = await sut.SearchAsync(searchTerm, 1, CancellationToken.None);

            Assert.AreEqual(0, absolute.Count);
        }

        [TestMethod]
        public async Task SearchAsync_EmptyString_DoesNotPerformSearch()
        {
            UnpkgCatalog sut = SetupCatalog();

            IReadOnlyList<ILibraryGroup> absolute = await sut.SearchAsync("", 1, CancellationToken.None);

            Assert.AreEqual(0, absolute.Count);
        }

        [TestMethod]
        public async Task SearchAsync_NullString()
        {
            UnpkgCatalog sut = SetupCatalog();

            IReadOnlyList<ILibraryGroup> absolute = await sut.SearchAsync(null, 1, CancellationToken.None);

            Assert.AreEqual(0, absolute.Count);
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
            UnpkgCatalog sut = SetupCatalog();

            CompletionSet result = await sut.GetLibraryCompletionSetAsync("jquery", 0);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(6, result.Length);
            Assert.AreEqual(100, result.Completions.Count());
            Assert.AreEqual("jquery", result.Completions.First().DisplayText);
            Assert.IsTrue(result.Completions.First().InsertionText.StartsWith("jquery"));
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
        public async Task GetLibraryCompletionSetAsync_WithNullNpmPackageInfoVersions_ReturnsNoCompletions()
        {
            UnpkgCatalog sut = SetupCatalog();

            CompletionSet result = await sut.GetLibraryCompletionSetAsync("@", 1);

            Assert.AreEqual(1, result.Start);
            Assert.AreEqual(0, result.Length);
            Assert.AreEqual(0, result.Completions.Count());
            Assert.AreEqual(CompletionSortOrder.Version, result.CompletionType);
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
