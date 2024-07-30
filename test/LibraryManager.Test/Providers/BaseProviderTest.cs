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
                },
            };

            _catalog = new Mocks.LibraryCatalog()
                .AddLibrary(_library);
        }

        [TestMethod]
        public async Task GenerateGoalState_NoFileMapping_SpecifyFilesAtLibraryLevel()
        {
            ILibraryInstallationState installState = new LibraryInstallationState
            {
                Name = "test",
                Version = "1.0",
                ProviderId = "TestProvider",
                DestinationPath = "lib/test",
                Files = ["folder/*.txt"],
            };
            BaseProvider provider = new TestProvider(_hostInteraction, cacheService: null)
            {
                Catalog = _catalog,
            };
            string expectedDestinationFile1 = FileHelpers.NormalizePath(Path.Combine(provider.HostInteraction.WorkingDirectory, "lib/test/folder/file3.txt"));
            string expectedSourceFile1 = FileHelpers.NormalizePath(Path.Combine(provider.HostInteraction.CacheDirectory, "TestProvider/test/1.0/folder/file3.txt"));

            OperationResult<LibraryInstallationGoalState> getGoalState = await provider.GetInstallationGoalStateAsync(installState, CancellationToken.None);

            Assert.IsTrue(getGoalState.Success);
            LibraryInstallationGoalState goalState = getGoalState.Result;
            Assert.IsNotNull(goalState);
            Assert.AreEqual(1, goalState.InstalledFiles.Count);
            Assert.IsTrue(goalState.InstalledFiles.TryGetValue(expectedDestinationFile1, out string file1));
            Assert.AreEqual(expectedSourceFile1, file1);
        }

        [TestMethod]
        public async Task GenerateGoalState_NoFileMapping_NoFilesAtLibraryLevel()
        {
            ILibraryInstallationState installState = new LibraryInstallationState
            {
                Name = "test",
                Version = "1.0",
                ProviderId = "TestProvider",
                DestinationPath = "lib/test",
            };
            BaseProvider provider = new TestProvider(_hostInteraction, cacheService: null)
            {
                Catalog = _catalog,
            };
            string expectedDestinationFile1 = FileHelpers.NormalizePath(Path.Combine(provider.HostInteraction.WorkingDirectory, "lib/test/file1.txt"));
            string expectedSourceFile1 = FileHelpers.NormalizePath(Path.Combine(provider.HostInteraction.CacheDirectory, "TestProvider/test/1.0/file1.txt"));
            string expectedDestinationFile2 = FileHelpers.NormalizePath(Path.Combine(provider.HostInteraction.WorkingDirectory, "lib/test/file2.txt"));
            string expectedSourceFile2 = FileHelpers.NormalizePath(Path.Combine(provider.HostInteraction.CacheDirectory, "TestProvider/test/1.0/file2.txt"));
            string expectedDestinationFile3 = FileHelpers.NormalizePath(Path.Combine(provider.HostInteraction.WorkingDirectory, "lib/test/folder/file3.txt"));
            string expectedSourceFile3 = FileHelpers.NormalizePath(Path.Combine(provider.HostInteraction.CacheDirectory, "TestProvider/test/1.0/folder/file3.txt"));

            OperationResult<LibraryInstallationGoalState> getGoalState = await provider.GetInstallationGoalStateAsync(installState, CancellationToken.None);

            Assert.IsTrue(getGoalState.Success);
            LibraryInstallationGoalState goalState = getGoalState.Result;

            Assert.IsNotNull(goalState);
            Assert.AreEqual(3, goalState.InstalledFiles.Count);
            Assert.IsTrue(goalState.InstalledFiles.TryGetValue(expectedDestinationFile1, out string file1));
            Assert.AreEqual(expectedSourceFile1, file1);
            Assert.IsTrue(goalState.InstalledFiles.TryGetValue(expectedDestinationFile2, out string file2));
            Assert.AreEqual(expectedSourceFile2, file2);
            Assert.IsTrue(goalState.InstalledFiles.TryGetValue(expectedDestinationFile3, out string file3));
            Assert.AreEqual(expectedSourceFile3, file3);
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
