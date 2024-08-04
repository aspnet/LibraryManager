// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Cache;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Providers;
using Microsoft.Web.LibraryManager.Resources;

namespace Microsoft.Web.LibraryManager.Test.Providers
{
    [TestClass]
    public class BaseProviderTest
    {
        private IHostInteraction _hostInteraction;
        private ILibrary _library;
        private readonly Mocks.LibraryCatalog _catalog;
        private readonly BaseProvider _provider;

        public BaseProviderTest()
        {
            _hostInteraction = new Mocks.HostInteraction()
            {
                CacheDirectory = "C:\\cache",
                WorkingDirectory = "C:\\project",
            };

            _library = new Mocks.Library()
            {
                Name = "test",
                Version = "1.0",
                ProviderId = "TestProvider",
                Files = new Dictionary<string, bool>()
                {
                    { "file1.txt", true },
                    { "file2.txt", false },
                    { "folder/file3.txt", false },
                    { "folder/subfolder/file4.txt", false },
                },
            };

            _catalog = new Mocks.LibraryCatalog()
                .AddLibrary(_library);

            _provider = new TestProvider(_hostInteraction, cacheService: null)
            {
                Catalog = _catalog,
            };
        }

        [TestMethod]
        public async Task GetInstallationGoalStateAsync_NoFileMapping_SpecifyFilesAtLibraryLevel()
        {
            ILibraryInstallationState installState = new LibraryInstallationState
            {
                Name = "test",
                Version = "1.0",
                ProviderId = "TestProvider",
                DestinationPath = "lib/test",
                Files = ["folder/*.txt"],
            };
            Dictionary<string, string> expectedFiles = new()
            {
                { "lib/test/folder/file3.txt", "TestProvider/test/1.0/folder/file3.txt" },
            };

            OperationResult<LibraryInstallationGoalState> getGoalState = await _provider.GetInstallationGoalStateAsync(installState, CancellationToken.None);

            Assert.IsTrue(getGoalState.Success);
            LibraryInstallationGoalState goalState = getGoalState.Result;
            Assert.IsNotNull(goalState);
            VerifyGoalStateMappings(goalState, expectedFiles);
        }

        [TestMethod]
        public async Task GetInstallationGoalStateAsync_NoFileMapping_NoFilesAtLibraryLevel()
        {
            ILibraryInstallationState installState = new LibraryInstallationState
            {
                Name = "test",
                Version = "1.0",
                ProviderId = "TestProvider",
                DestinationPath = "lib/test",
            };
            Dictionary<string, string> expectedFiles = new()
            {
                { "lib/test/file1.txt", "TestProvider/test/1.0/file1.txt" },
                { "lib/test/file2.txt", "TestProvider/test/1.0/file2.txt" },
                { "lib/test/folder/file3.txt", "TestProvider/test/1.0/folder/file3.txt" },
                { "lib/test/folder/subfolder/file4.txt", "TestProvider/test/1.0/folder/subfolder/file4.txt" },
            };

            OperationResult<LibraryInstallationGoalState> getGoalState = await _provider.GetInstallationGoalStateAsync(installState, CancellationToken.None);

            Assert.IsTrue(getGoalState.Success);
            LibraryInstallationGoalState goalState = getGoalState.Result;
            Assert.IsNotNull(goalState);
            VerifyGoalStateMappings(goalState, expectedFiles);
        }

        [TestMethod]
        public async Task GetInstallationGoalStateAsync_FileMapping_NoRootPath_LiteralFile()
        {
            ILibraryInstallationState installState = new LibraryInstallationState
            {
                Name = "test",
                Version = "1.0",
                ProviderId = "TestProvider",
                DestinationPath = "lib/test",
                FileMappings = [
                    new FileMapping
                    {
                        Files = ["file1.txt"],
                    },
                ],
            };
            Dictionary<string, string> expectedFiles = new()
            {
                { "lib/test/file1.txt", "TestProvider/test/1.0/file1.txt" },
            };

            OperationResult<LibraryInstallationGoalState> getGoalState = await _provider.GetInstallationGoalStateAsync(installState, CancellationToken.None);

            Assert.IsTrue(getGoalState.Success);
            LibraryInstallationGoalState goalState = getGoalState.Result;
            Assert.IsNotNull(goalState);
            VerifyGoalStateMappings(goalState, expectedFiles);
        }

        [TestMethod]
        public async Task GetInstallationGoalStateAsync_FileMapping_NoRootPath_FileGlob()
        {
            ILibraryInstallationState installState = new LibraryInstallationState
            {
                Name = "test",
                Version = "1.0",
                ProviderId = "TestProvider",
                DestinationPath = "lib/test",
                FileMappings = [
                    new FileMapping
                    {
                        Files = ["*.txt"],
                    },
                ],
            };
            Dictionary<string, string> expectedFiles = new()
            {
                { "lib/test/file1.txt", "TestProvider/test/1.0/file1.txt" },
                { "lib/test/file2.txt", "TestProvider/test/1.0/file2.txt" },
            };

            OperationResult<LibraryInstallationGoalState> getGoalState = await _provider.GetInstallationGoalStateAsync(installState, CancellationToken.None);

            Assert.IsTrue(getGoalState.Success);
            LibraryInstallationGoalState goalState = getGoalState.Result;
            Assert.IsNotNull(goalState);
            VerifyGoalStateMappings(goalState, expectedFiles);
        }

        [TestMethod]
        public async Task GetInstallationGoalStateAsync_FileMapping_NoRootPath_FileGlobExclude()
        {
            ILibraryInstallationState installState = new LibraryInstallationState
            {
                Name = "test",
                Version = "1.0",
                ProviderId = "TestProvider",
                DestinationPath = "lib/test",
                FileMappings = [
                    new FileMapping
                    {
                        Files = ["**/*.txt", "!file2.txt"],
                    },
                ],
            };
            Dictionary<string, string> expectedFiles = new()
            {
                { "lib/test/file1.txt", "TestProvider/test/1.0/file1.txt" },
                { "lib/test/folder/file3.txt", "TestProvider/test/1.0/folder/file3.txt" },
                { "lib/test/folder/subfolder/file4.txt", "TestProvider/test/1.0/folder/subfolder/file4.txt" },
            };

            OperationResult<LibraryInstallationGoalState> getGoalState = await _provider.GetInstallationGoalStateAsync(installState, CancellationToken.None);

            Assert.IsTrue(getGoalState.Success);
            LibraryInstallationGoalState goalState = getGoalState.Result;
            Assert.IsNotNull(goalState);
            VerifyGoalStateMappings(goalState, expectedFiles);
        }

        [TestMethod]
        public async Task GetInstallationGoalStateAsync_FileMapping_WithRootPath_Glob()
        {
            ILibraryInstallationState installState = new LibraryInstallationState
            {
                Name = "test",
                Version = "1.0",
                ProviderId = "TestProvider",
                DestinationPath = "lib/test",
                FileMappings = [
                    new FileMapping
                    {
                        Root = "folder",
                        Files = ["file3.txt"],
                    },
                ],
            };
            Dictionary<string, string> expectedFiles = new()
            {
                { "lib/test/file3.txt", "TestProvider/test/1.0/folder/file3.txt" },
            };

            OperationResult<LibraryInstallationGoalState> getGoalState = await _provider.GetInstallationGoalStateAsync(installState, CancellationToken.None);

            Assert.IsTrue(getGoalState.Success);
            LibraryInstallationGoalState goalState = getGoalState.Result;
            Assert.IsNotNull(goalState);
            VerifyGoalStateMappings(goalState, expectedFiles);
        }

        [TestMethod]
        public async Task GetInstallationGoalStateAsync_FileMapping_WithRootPath()
        {
            ILibraryInstallationState installState = new LibraryInstallationState
            {
                Name = "test",
                Version = "1.0",
                ProviderId = "TestProvider",
                DestinationPath = "lib/test",
                FileMappings = [
                    new FileMapping
                    {
                        Root = "folder",
                        Files = ["**/*.txt"],
                    },
                ],
            };
            Dictionary<string, string> expectedFiles = new()
            {
                { "lib/test/file3.txt", "TestProvider/test/1.0/folder/file3.txt" },
                { "lib/test/subfolder/file4.txt", "TestProvider/test/1.0/folder/subfolder/file4.txt" },
            };

            OperationResult<LibraryInstallationGoalState> getGoalState = await _provider.GetInstallationGoalStateAsync(installState, CancellationToken.None);

            Assert.IsTrue(getGoalState.Success);
            LibraryInstallationGoalState goalState = getGoalState.Result;
            Assert.IsNotNull(goalState);
            VerifyGoalStateMappings(goalState, expectedFiles);
        }

        [TestMethod]
        public async Task GetInstallationGoalStateAsync_FileMapping_SeparateDestinations()
        {
            ILibraryInstallationState installState = new LibraryInstallationState
            {
                Name = "test",
                Version = "1.0",
                ProviderId = "TestProvider",
                DestinationPath = "lib/test",
                FileMappings = [
                    new FileMapping
                    {
                        Files = ["file1.txt"],
                        Destination = "dest1",
                    },
                    new FileMapping
                    {
                        Files = ["*.txt"],
                        Destination = "dest2",
                    },
                ],
            };
            Dictionary<string, string> expectedFiles = new()
            {
                { "dest1/file1.txt", "TestProvider/test/1.0/file1.txt" },
                { "dest2/file1.txt", "TestProvider/test/1.0/file1.txt" },
                { "dest2/file2.txt", "TestProvider/test/1.0/file2.txt" },
            };

            OperationResult<LibraryInstallationGoalState> getGoalState = await _provider.GetInstallationGoalStateAsync(installState, CancellationToken.None);

            Assert.IsTrue(getGoalState.Success);
            LibraryInstallationGoalState goalState = getGoalState.Result;
            Assert.IsNotNull(goalState);
            VerifyGoalStateMappings(goalState, expectedFiles);
        }

        [TestMethod]
        public async Task GetInstallationGoalStateAsync_FileMapping_BadFile_Fails()
        {
            ILibraryInstallationState installState = new LibraryInstallationState
            {
                Name = "test",
                Version = "1.0",
                ProviderId = "TestProvider",
                DestinationPath = "lib/test",
                FileMappings = [
                    new FileMapping
                    {
                        Files = ["file1.txt", "file4.txt"],
                    },
                ],
            };
            Dictionary<string, string> expectedFiles = new()
            {
                { "lib/test/file1.txt", "TestProvider/test/1.0/file1.txt" },
            };

            OperationResult<LibraryInstallationGoalState> getGoalState = await _provider.GetInstallationGoalStateAsync(installState, CancellationToken.None);

            Assert.IsFalse(getGoalState.Success);
            Assert.AreEqual("LIB018", getGoalState.Errors[0].Code);
            Assert.IsNull(getGoalState.Result);
        }

        [TestMethod]
        public async Task GetInstallationGoalStateAsync_FileMapping_InvalidDestination_Fails()
        {
            ILibraryInstallationState installState = new LibraryInstallationState
            {
                Name = "test",
                Version = "1.0",
                ProviderId = "TestProvider",
                DestinationPath = "lib/test",
                FileMappings = [
                    new FileMapping
                    {
                        Files = ["file1.txt"],
                        Destination = "../file1.txt",
                    },
                ],
            };

            OperationResult<LibraryInstallationGoalState> getGoalState = await _provider.GetInstallationGoalStateAsync(installState, CancellationToken.None);

            Assert.IsFalse(getGoalState.Success);
            Assert.AreEqual("LIB008", getGoalState.Errors[0].Code);
            Assert.IsNull(getGoalState.Result);
        }

        [TestMethod]
        public async Task GetInstallationGoalStateAsync_FileMapping_DestinationConflict_Fails()
        {
            ILibraryInstallationState installState = new LibraryInstallationState
            {
                Name = "test",
                Version = "1.0",
                ProviderId = "TestProvider",
                DestinationPath = "lib/test",
                FileMappings = [
                    new FileMapping
                    {
                        Files = ["file1.txt"],
                    },
                    new FileMapping
                    {
                        // this will conflict with file1.txt
                        Files = ["*.txt"],
                    },
                ],
            };

            OperationResult<LibraryInstallationGoalState> getGoalState = await _provider.GetInstallationGoalStateAsync(installState, CancellationToken.None);

            Assert.IsFalse(getGoalState.Success);
            Assert.AreEqual("LIB017", getGoalState.Errors[0].Code);
            Assert.IsNull(getGoalState.Result);
        }

        private void VerifyGoalStateMappings(LibraryInstallationGoalState goalState, Dictionary<string, string> expectedFiles)
        {
            Assert.AreEqual(expectedFiles.Count, goalState.InstalledFiles.Count);

            foreach (KeyValuePair<string, string> kvp in expectedFiles)
            {
                string expectedDestinationFile = FileHelpers.NormalizePath(Path.Combine(_provider.HostInteraction.WorkingDirectory, kvp.Key));
                string expectedSourceFile = FileHelpers.NormalizePath(Path.Combine(_provider.HostInteraction.CacheDirectory, kvp.Value));

                Assert.IsTrue(goalState.InstalledFiles.TryGetValue(expectedDestinationFile, out string file), $"failed to find expected destination file: {expectedDestinationFile}");
                Assert.AreEqual(expectedSourceFile, file);
            }
        }

        private class TestProvider : BaseProvider
        {
            public TestProvider(IHostInteraction hostInteraction, CacheService cacheService)
                : base(hostInteraction, cacheService)
            {
            }

            public override string Id => nameof(TestProvider);

            public override string LibraryIdHintText => Text.CdnjsLibraryIdHintText;

            public ILibraryCatalog Catalog { get; set; }

            public override ILibraryCatalog GetCatalog() => Catalog;

            public override string GetSuggestedDestination(ILibrary library)
            {
                throw new NotImplementedException();
            }

            protected override string GetDownloadUrl(ILibraryInstallationState state, string sourceFile)
            {
                throw new NotImplementedException();
            }
        }
    }
}
