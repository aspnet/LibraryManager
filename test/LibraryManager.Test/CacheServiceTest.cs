// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Mocks;
using Microsoft.Web.LibraryManager.Providers.Cdnjs;
using Microsoft.Web.LibraryManager.Providers.FileSystem;

namespace Microsoft.Web.LibraryManager.Test
{
    [TestClass]
    public class CacheServiceTest
    {

        private string _filePath;
        private string _cacheFolder;
        private string _projectFolder;
        private string _catalogCacheFile;
        private IDependencies _dependencies;
        private HostInteraction _hostInteraction;
        private CacheService _cacheService;

        [TestInitialize]
        public void Setup()
        {
            _cacheFolder = Environment.ExpandEnvironmentVariables(@"%localappdata%\Microsoft\Library\");
            _catalogCacheFile = Path.Combine(_cacheFolder, "TestCatalog.json");
            _projectFolder = Path.Combine(Path.GetTempPath(), "LibraryManager");
            _filePath = Path.Combine(_projectFolder, "libman.json");

            _hostInteraction = new HostInteraction(_projectFolder, _cacheFolder);
            _dependencies = new Dependencies(_hostInteraction, new CdnjsProviderFactory(), new FileSystemProviderFactory());
            _cacheService = new CacheService(new Mocks.WebRequestHandler());

            Directory.CreateDirectory(_projectFolder);
        }

        [TestCleanup]
        public void Cleanup()
        {
            TestUtils.DeleteDirectoryWithRetries(_projectFolder);
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public async Task GetCatalogAsync_ThrowsForInvalidCacheFilePath()
        {
            string validUrl = "Valid Url";

            string content = await _cacheService.GetCatalogAsync(validUrl, "invalid path", CancellationToken.None);
        }

        [TestMethod]
        public async Task GetCatalogAsync_WritesToCache()
        {
            string validUrl = "Valid Url";
            string content = await _cacheService.GetCatalogAsync(validUrl, _catalogCacheFile, CancellationToken.None);

            Assert.IsTrue(!string.IsNullOrEmpty(content));
            Assert.IsTrue(File.Exists(_catalogCacheFile));
        }

        [TestMethod]
        public async Task GetCatalogAsync_ReadsFromCache()
        {
            string invalidUrl = "Invalid Url";
            string content = await _cacheService.GetCatalogAsync(invalidUrl, _catalogCacheFile, CancellationToken.None);

            Assert.IsTrue(!string.IsNullOrEmpty(content));
            Assert.IsTrue(File.Exists(_catalogCacheFile));
        }

        [TestMethod]
        public async Task GetCatalogAsync_ReadsFromCache_IfNotExpired()
        {
            string validUrl = "Valid Url";

            // setup
            await _cacheService.GetCatalogAsync(validUrl, _catalogCacheFile, CancellationToken.None);
            DateTime beforeCacheFileDT = File.GetCreationTime(_catalogCacheFile);

            // act
            await _cacheService.GetCatalogAsync(validUrl, _catalogCacheFile, CancellationToken.None);
            DateTime afterCacheFileDT = File.GetCreationTime(_catalogCacheFile);

            // verify
            Assert.IsTrue(beforeCacheFileDT.Equals(afterCacheFileDT));
            Assert.IsTrue(File.Exists(_catalogCacheFile));
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task GetCatalogAsync_Throws_OperationCanceled_WhenCancelled()
        {
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            CancellationTokenSource tokenSource = new CancellationTokenSource();

            string invalidUrl = "Invalid Url";

            tokenSource.Cancel();
            await _cacheService.GetCatalogAsync(invalidUrl, _catalogCacheFile, tokenSource.Token);

        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task HydrateCache_Throws_OperationCanceled_WhenCancelled()
        {
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            string libraryFile1_Path = Path.Combine(_cacheFolder, "Library1", "file1.txt");
            string libraryFile2_Path = Path.Combine(_cacheFolder, "Library1", "file2.txt");
            string validUrl = "";
            string destinationFile = _cacheFolder;

            List<CacheServiceMetadata> desiredCasheFiles = new List<CacheServiceMetadata>()
            {
                new CacheServiceMetadata(validUrl, libraryFile1_Path),
                new CacheServiceMetadata(validUrl, libraryFile2_Path)
            };

            tokenSource.Cancel();
            await _cacheService.RefreshCacheAsync(desiredCasheFiles, tokenSource.Token);

        }

        [TestMethod]
        public async Task HydrateCache_WriteLibraryFilesToCacheFolder()
        {
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            string libraryFile1_Path = Path.Combine(_cacheFolder, "Library1", "file1.txt");
            string libraryFile2_Path = Path.Combine(_cacheFolder, "Library1", "file2.txt");
            string validUrl = "";
            string destinationFile = _cacheFolder;

            List<CacheServiceMetadata> desiredCasheFiles = new List<CacheServiceMetadata>()
            {
                new CacheServiceMetadata(validUrl, libraryFile1_Path),
                new CacheServiceMetadata(validUrl, libraryFile2_Path)
            };

            // act 
            await _cacheService.RefreshCacheAsync(desiredCasheFiles, CancellationToken.None);

            // verify
            Assert.IsTrue(File.Exists(libraryFile1_Path));
            Assert.IsTrue(File.Exists(libraryFile2_Path));
        }
    }
}
