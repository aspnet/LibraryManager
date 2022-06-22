// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Contracts.Caching;
using Microsoft.Web.LibraryManager.LibraryNaming;
using Microsoft.Web.LibraryManager.Mocks;
using Microsoft.Web.LibraryManager.Providers.Cdnjs;
using Moq;

namespace Microsoft.Web.LibraryManager.Test.Providers.Cdnjs
{
    [TestClass]
    public class CdnjsCatalogTest
    {
        private readonly List<string> _prepopulatedFiles = new List<string>();

        private CdnjsCatalog SetupCatalog(ICacheService testCacheService = null, Dictionary<string, string> prepopulateCacheFiles = null)
        {
            string projectFolder = Path.Combine(Path.GetTempPath(), "LibraryManager");
            string cacheFolder = Environment.ExpandEnvironmentVariables(@"%localappdata%\Microsoft\Library\");
            var hostInteraction = new HostInteraction(projectFolder, cacheFolder);
            ICacheService cacheService = testCacheService ?? new Mock<ICacheService>().SetupCatalog()
                                                                                      .SetupSampleLibrary()
                                                                                      .Object;

                if (prepopulateCacheFiles != null)
            {
                foreach (KeyValuePair<string, string> item in prepopulateCacheFiles)
                {
                    // put the provider IdText into the path to mimic the provider implementation
                    string filePath = Path.Combine(cacheFolder, CdnjsProvider.IdText, item.Key);
                    string directoryPath = Path.GetDirectoryName(filePath);
                    Directory.CreateDirectory(directoryPath);
                    File.WriteAllText(filePath, item.Value);
                    _prepopulatedFiles.Add(filePath);
                }
            }

            var provider = new CdnjsProvider(hostInteraction, cacheService:null);
            return new CdnjsCatalog(provider, cacheService, new VersionedLibraryNamingScheme());
        }

        [TestCleanup]
        public void CleanupPrepopulatedFiles()
        {
            foreach (string file in _prepopulatedFiles)
            {
                File.Delete(file);
            }
        }

        [DataTestMethod]
        [DataRow("sample", "sampleLibrary")]
        public async Task SearchAsync_Success_SingleMatch(string searchTerm, string expectedId)
        {
            CdnjsCatalog sut = SetupCatalog();

            IReadOnlyList<ILibraryGroup> absolute = await sut.SearchAsync(searchTerm, 2, CancellationToken.None);
            Assert.AreEqual(1, absolute.Count);

            IEnumerable<string> versions = await absolute[0].GetLibraryVersions(CancellationToken.None);
            Assert.IsTrue(versions.Any());

            ILibrary library = await sut.GetLibraryAsync(absolute[0].DisplayName, versions.First(), CancellationToken.None);
            Assert.IsTrue(library.Files.Count > 0);
            Assert.AreEqual(expectedId, library.Name);
            Assert.AreEqual(1, library.Files.Count(f => f.Value));
            Assert.IsNotNull(library.Name);
            Assert.IsNotNull(library.Version);
            Assert.AreEqual(CdnjsProvider.IdText, library.ProviderId);
        }

        [TestMethod]
        public async Task SearchAsync_MultipleMatches()
        {
            CdnjsCatalog sut = SetupCatalog();

            IReadOnlyList<ILibraryGroup> result = await sut.SearchAsync(term:"test", 5, CancellationToken.None);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("test-library", result.First().DisplayName);
            Assert.AreEqual("test-library2", result.Last().DisplayName);
        }

        [TestMethod]
        public async Task SearchAsync_NoHits()
        {
            CdnjsCatalog sut = SetupCatalog();

            IReadOnlyList<ILibraryGroup> absolute = await sut.SearchAsync(@"*9)_-", 1, CancellationToken.None);

            Assert.AreEqual(0, absolute.Count);
        }

        [TestMethod]
        public async Task SearchAsync_EmptyString()
        {
            CdnjsCatalog sut = SetupCatalog();

            IReadOnlyList<ILibraryGroup> absolute = await sut.SearchAsync("", 1, CancellationToken.None);

            Assert.AreEqual(1, absolute.Count);
        }

        [TestMethod]
        public async Task SearchAsync_NullString()
        {
            CdnjsCatalog sut = SetupCatalog();

            IReadOnlyList<ILibraryGroup> absolute = await sut.SearchAsync(null, 1, CancellationToken.None);

            Assert.AreEqual(1, absolute.Count);
        }

        [TestMethod]
        public async Task GetLibraryAsync_Success()
        {
            CdnjsCatalog sut = SetupCatalog();

            ILibrary library = await sut.GetLibraryAsync("sampleLibrary", "3.1.4", CancellationToken.None);

            Assert.IsNotNull(library);
            Assert.AreEqual("sampleLibrary", library.Name);
            Assert.AreEqual("3.1.4", library.Version);
            Assert.IsNotNull(library.Files);
        }

        [TestMethod]
        public async Task GetLibraryAsync_InvalidLibraryId()
        {
            CdnjsCatalog sut = SetupCatalog();

            await Assert.ThrowsExceptionAsync<InvalidLibraryException>(async () =>
                await sut.GetLibraryAsync("invalid_id", "invalid_version", CancellationToken.None));
        }

        [TestMethod]
        public async Task GetLibraryAsync_WebRequestFailsAndNoCachedMetadata_ThrowsInvalidLibraryId()
        {
            var fakeCacheService = new Mock<ICacheService>();
            fakeCacheService.Setup(x => x.GetContentsFromUriWithCacheFallbackAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                            .Throws(new ResourceDownloadException("Cache download failed."));
            CdnjsCatalog sut = SetupCatalog(fakeCacheService.Object);

            await Assert.ThrowsExceptionAsync<InvalidLibraryException>(async () =>
                await sut.GetLibraryAsync("invalid_id", "invalid_version", CancellationToken.None));
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_Names()
        {
            CdnjsCatalog sut = SetupCatalog();

            CompletionSet result = await sut.GetLibraryCompletionSetAsync("test", 0);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(4, result.Length);
            Assert.AreEqual(2, result.Completions.Count());
            Assert.AreEqual("test-library", result.Completions.First().DisplayText);
            Assert.AreEqual("test-library@1.0.0", result.Completions.First().InsertionText);
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_Versions()
        {
            CdnjsCatalog sut = SetupCatalog();

            CompletionSet result = await sut.GetLibraryCompletionSetAsync("sampleLibrary@", 14);

            Assert.AreEqual("sampleLibrary@".Length, result.Start);
            Assert.AreEqual(0, result.Length);
            Assert.AreEqual(5, result.Completions.Count());
            Assert.AreEqual("2.0.0", result.Completions.Last().DisplayText);
            Assert.AreEqual("sampleLibrary@2.0.0", result.Completions.Last().InsertionText);
        }

        [TestMethod]
        public async Task GetLatestVersion_LatestExist()
        {
            CdnjsCatalog sut = SetupCatalog();

            const string libraryName = "sampleLibrary";
            const string expectedVersion = "3.1.4";
            string result = await sut.GetLatestVersion(libraryName, false, CancellationToken.None);

            Assert.AreEqual(expectedVersion, result);
        }

        [TestMethod]
        public async Task GetLatestVersion_PreRelease()
        {
            CdnjsCatalog sut = SetupCatalog();

            const string libraryName = "sampleLibrary";
            const string oldVersion = "4.0.0-beta.2";
            string result = await sut.GetLatestVersion(libraryName, true, CancellationToken.None);

            Assert.AreEqual(oldVersion, result);
        }

        [TestMethod]
        public void ConvertToLibraryGroup_ValidJsonCatalog()
        {
            CdnjsCatalog sut = SetupCatalog();
            string json = @"{""results"":[{""name"":""1140"",""latest"":""https://cdnjs.cloudflare.com/ajax/libs/1140/2.0/1140.min.css"",
""description"":""The 1140 grid fits perfectly into a 1280 monitor. On smaller monitors it becomes fluid and adapts to the width of the browser.""
,""version"":""2.0""}],""total"":1}";

            IEnumerable<CdnjsLibraryGroup> libraryGroup = sut.ConvertToLibraryGroups(json);

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
            CdnjsCatalog sut = SetupCatalog();

            IEnumerable<CdnjsLibraryGroup> libraryGroup = sut.ConvertToLibraryGroups(json);

            Assert.IsNull(libraryGroup);
        }

        [TestMethod]
        public async Task SearchAsync_CacheDownloadFailsWhenNoCacheFileExists_FindsNoMatches()
        {
            var fakeCacheService = new Mock<ICacheService>();
            fakeCacheService.Setup(x => x.GetContentsFromUriWithCacheFallbackAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                            .Throws(new ResourceDownloadException("Cache download failed."));
            CdnjsCatalog sut = SetupCatalog(fakeCacheService.Object);

            IReadOnlyList<ILibraryGroup> result = await sut.SearchAsync("test", 5, CancellationToken.None);

            Assert.AreEqual(0, result.Count);
        }
    }

    public static class CndjsCatalogTestSetups
    {
        public static Mock<ICacheService> SetupCatalog(this Mock<ICacheService> cacheService)
        {
            cacheService.Setup(x => x.GetContentsFromUriWithCacheFallbackAsync(It.Is<string>(s => s.Equals(CdnjsCatalog.CatalogUrl, StringComparison.OrdinalIgnoreCase)),
                                                                               It.IsAny<string>(),
                                                                               It.IsAny<CancellationToken>()))
                        .Returns(Task.FromResult(FakeCatalogContents));

            return cacheService;
        }

        public static Mock<ICacheService> SetupSampleLibrary(this Mock<ICacheService> cacheService)
        {
            string metadataUrl = string.Format(CdnjsCatalog.MetaPackageUrlFormat, "sampleLibrary");
            cacheService.Setup(x => x.GetContentsFromUriWithCacheFallbackAsync(It.Is<string>(s => s.Equals(metadataUrl, StringComparison.OrdinalIgnoreCase)),
                                                                               It.IsAny<string>(),
                                                                               It.IsAny<CancellationToken>()))
                        .Returns(Task.FromResult(FakeLibraryMetadata));

            string libraryMetadataUrl = string.Format(CdnjsCatalog.PackageVersionUrlFormat, "sampleLibrary", "4.0.0-beta.1");
            cacheService.Setup(x => x.GetContentsFromUriWithCacheFallbackAsync(It.Is<string>(s => s.Equals(libraryMetadataUrl, StringComparison.OrdinalIgnoreCase)),
                                                                               It.IsAny<string>(),
                                                                               It.IsAny<CancellationToken>()))
                        .Returns(Task.FromResult(GenerateFakeLibraryFiles(
                            "sample/js/sampleLibrary.js",
                            "sample/js/sampleLibrary.min.js")));

            return cacheService;
        }

        /// <summary>
        /// Mock contents containing multiple libraries with different versions
        /// </summary>
        public const string FakeCatalogContents = @"{
    ""results"": [
        {
            ""name"": ""sampleLibrary"",
            ""latest"": ""https://test-library.com/sample/js/sampleLibrary.min.js"",
            ""description"": ""A sample library for testing"",
            ""version"": ""3.1.4""
        },
        {
            ""name"": ""test-library"",
            ""latest"": ""https://test-library.com/test-library.min.js"",
            ""description"": ""A fake library for testing"",
            ""version"": ""1.0.0""
        },
        {
            ""name"": ""test-library2"",
            ""latest"": ""https://test-library.com/test-library2.min.js"",
            ""description"": ""A second fake library for testing"",
            ""version"": ""2.0.0""
        }
    ],
    ""total"": 3
}";

        /// <summary>
        /// Single library metadata, containing multiple releases including preview releases
        /// </summary>
        public const string FakeLibraryMetadata = @"{
    ""name"": ""sampleLibrary"",
    ""filename"": ""sample/js/sampleLibrary.min.js"",
    ""version"": ""3.1.4"",
    ""description"": ""Sample library for test input"",
    ""versions"": [
        ""4.0.0-beta.1"",
        ""4.0.0-beta.2"",
        ""4.0.0-beta.10"",
        ""3.1.4"",
        ""2.0.0"",
    ]
}";

        public static string GenerateFakeLibraryFiles(params string[] filenames)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"{ ""files"": [ """);
            sb.Append(string.Join(@""", """, filenames));
            sb.Append(@""" ] }");

            return sb.ToString();
        }
    }
}
