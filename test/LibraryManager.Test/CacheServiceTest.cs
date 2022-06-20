// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Cache;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Mocks;
using Moq;

namespace Microsoft.Web.LibraryManager.Test
{
    [TestClass]
    public class CacheServiceTest
    {

        private string _cacheFolder;
        private string _projectFolder;
        private CacheService _cacheService;

        [TestInitialize]
        public void Setup()
        {
            _cacheFolder = Environment.ExpandEnvironmentVariables(@"%localappdata%\Microsoft\Library\");
            _projectFolder = Path.Combine(Path.GetTempPath(), "LibraryManager");
            _cacheService = new CacheService(new Mocks.WebRequestHandler());

            Directory.CreateDirectory(_projectFolder);
            Directory.CreateDirectory(_cacheFolder);
        }

        [TestCleanup]
        public void Cleanup()
        {
            TestUtils.DeleteDirectoryWithRetries(_projectFolder);
            TestUtils.DeleteDirectoryWithRetries(_cacheFolder);
        }

        [TestMethod]
        public async Task HydrateCache_Throws_OperationCanceled_WhenCancelled()
        {
            var logger = new Logger();
            var tokenSource = new CancellationTokenSource();
            string libraryFile1_Path = Path.Combine(_cacheFolder, "Library1", "file1.txt");
            string libraryFile2_Path = Path.Combine(_cacheFolder, "Library1", "file2.txt");
            string validUrl = "";

            var desiredCacheFiles = new List<CacheFileMetadata>()
            {
                new CacheFileMetadata(validUrl, libraryFile1_Path),
                new CacheFileMetadata(validUrl, libraryFile2_Path)
            };

            tokenSource.Cancel();
            await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
                await _cacheService.RefreshCacheAsync(desiredCacheFiles, logger, tokenSource.Token));

        }

        [TestMethod]
        public async Task HydrateCache_WriteLibraryFilesToCacheFolder()
        {
            var logger = new Logger();
            string libraryFile1_Path = Path.Combine(_cacheFolder, "Library1", "file1.txt");
            string libraryFile2_Path = Path.Combine(_cacheFolder, "Library1", "file2.txt");
            string validUrl = "";

            var desiredCasheFiles = new List<CacheFileMetadata>()
            {
                new CacheFileMetadata(validUrl, libraryFile1_Path),
                new CacheFileMetadata(validUrl, libraryFile2_Path)
            };

            // act 
            await _cacheService.RefreshCacheAsync(desiredCasheFiles, logger, CancellationToken.None);

            // verify
            Assert.IsTrue(File.Exists(libraryFile1_Path));
            Assert.IsTrue(File.Exists(libraryFile2_Path));
        }

        [TestMethod]
        public async Task GetContentsFromCachedFileWithWebRequestFallbackAsync_InvalidCacheFilePath_ShouldThrow()
        {
            await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () =>
                await _cacheService.GetContentsFromCachedFileWithWebRequestFallbackAsync("unrooted path", "http://example.com", CancellationToken.None));
        }

        [TestMethod]
        public async Task GetContentsFromCachedFileWithWebRequestFallbackAsync_WebRequestFails_ShouldThrow()
        {
            string cachePath = Path.Combine(_cacheFolder, "testfile.json");
            if (File.Exists(cachePath))
            {
                File.Delete(cachePath);
            }
            var fakeRequestHandler = new Mock<IWebRequestHandler>();
            fakeRequestHandler.Setup(x => x.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                              .Throws(new ResourceDownloadException("Request blocked for testing"));
            var sut = new CacheService(fakeRequestHandler.Object);

            await Assert.ThrowsExceptionAsync<ResourceDownloadException>(async () =>
                await sut.GetContentsFromCachedFileWithWebRequestFallbackAsync(cachePath, "any url", CancellationToken.None));
        }

        [TestMethod]
        public async Task GetContentsFromCachedFileWithWebRequestFallbackAsync_CacheFileExists_ShouldReturnFileContentsWithoutWebRequest()
        {
            string cachePath = Path.Combine(_cacheFolder, "testfile.json");
            File.WriteAllText(cachePath, "Test file");
            var mockRequestHandler = new Mock<IWebRequestHandler>();
            var sut = new CacheService(mockRequestHandler.Object);

            string result = await sut.GetContentsFromCachedFileWithWebRequestFallbackAsync(cachePath, "http://example.com", CancellationToken.None);
            File.Delete(cachePath);

            Assert.AreEqual("Test file", result);
            mockRequestHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task GetContentsFromCachedFileWithWebRequestFallbackAsync_CacheFileNotExists_ShouldMakeWebRequestAndSaveToFile()
        {
            string cachePath = Path.Combine(_cacheFolder, "testfile.json");
            if (File.Exists(cachePath))
            {
                File.Delete(cachePath);
            }
            var mockRequestHandler = new Mock<IWebRequestHandler>();
            mockRequestHandler.Setup(x => x.GetStreamAsync("http://example.com", It.IsAny<CancellationToken>()))
                              .Returns(Task.FromResult<Stream>(new MemoryStream(Encoding.Default.GetBytes("Test web request"))));
            var sut = new CacheService(mockRequestHandler.Object);

            string result = await sut.GetContentsFromCachedFileWithWebRequestFallbackAsync(cachePath, "http://example.com", CancellationToken.None);

            Assert.AreEqual("Test web request", result);
            Assert.IsTrue(File.Exists(cachePath));
            Assert.AreEqual("Test web request", File.ReadAllText(cachePath));
        }

        [TestMethod]
        public async Task GetContentsFromUriWithCacheFallbackAsync_WebRequestFailsAndNoCachedFile_ShouldThrow()
        {
            var fakeRequestHandler = new Mock<IWebRequestHandler>();
            fakeRequestHandler.Setup(x => x.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                              .Throws(new ResourceDownloadException("Request blocked for testing"));
            var sut = new CacheService(fakeRequestHandler.Object);

            await Assert.ThrowsExceptionAsync<ResourceDownloadException>(async () =>
                await sut.GetContentsFromUriWithCacheFallbackAsync("any url", "any file", CancellationToken.None));
        }

        [TestMethod]
        public async Task GetContentsFromUriWithCacheFallbackAsync_WebRequestFailsAndInvalidFileName_ShouldThrow()
        {
            var fakeRequestHandler = new Mock<IWebRequestHandler>();
            fakeRequestHandler.Setup(x => x.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                              .Throws(new ResourceDownloadException("Request blocked for testing"));
            var sut = new CacheService(fakeRequestHandler.Object);

            await Assert.ThrowsExceptionAsync<ResourceDownloadException>(async () =>
                await sut.GetContentsFromUriWithCacheFallbackAsync("any url", "any file", CancellationToken.None));
        }


        [TestMethod]
        public async Task GetContentsFromUriWithCacheFallbackAsync_WebRequestFailsAndHasCachedFile_ShouldReturnFileContents()
        {
            string cachePath = Path.Combine(_cacheFolder, "testfile.json");
            File.WriteAllText(cachePath, "Test file");
            var fakeRequestHandler = new Mock<IWebRequestHandler>();
            fakeRequestHandler.Setup(x => x.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                              .Throws(new ResourceDownloadException("Request blocked for testing"));
            var sut = new CacheService(fakeRequestHandler.Object);

            string result = await sut.GetContentsFromUriWithCacheFallbackAsync("any url", cachePath, CancellationToken.None);

            Assert.AreEqual("Test file", result);
        }

        [TestMethod]
        public async Task GetContentsFromUriWithCacheFallbackAsync_WebRequestSuccedsAndNoCachedFile_ShouldSaveContentsToCacheFile()
        {
            string cachePath = Path.Combine(_cacheFolder, "testfile.json");
            if (File.Exists(cachePath))
            {
                File.Delete(cachePath);
            }
            var fakeRequestHandler = new Mock<IWebRequestHandler>();
            fakeRequestHandler.Setup(x => x.GetStreamAsync("http://example.com", It.IsAny<CancellationToken>()))
                              .Returns(Task.FromResult<Stream>(new MemoryStream(Encoding.Default.GetBytes("Test request content"))));
            var sut = new CacheService(fakeRequestHandler.Object);

            string result = await sut.GetContentsFromUriWithCacheFallbackAsync("http://example.com", cachePath, CancellationToken.None);

            Assert.AreEqual("Test request content", result);
            Assert.IsTrue(File.Exists(cachePath));
            Assert.AreEqual("Test request content", File.ReadAllText(cachePath));
        }
    }
}
