// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
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

        }

        [TestMethod]
        public void GenerateGoalState_NoFileMapping_SpecifyFilesAtLibraryLevel()
        {
            ILibraryInstallationState installState = new LibraryInstallationState
            {
                Name = "test",
                Version = "1.0",
                ProviderId = "TestProvider",
                DestinationPath = "lib/test",
                Files = ["folder/*.txt"],
            };
            BaseProvider provider = new TestProvider(_hostInteraction, cacheService: null);
            string expectedDestinationFile1 = FileHelpers.NormalizePath(Path.Combine(provider.HostInteraction.WorkingDirectory, "lib/test/folder/file3.txt"));
            string expectedSourceFile1 = FileHelpers.NormalizePath(Path.Combine(provider.HostInteraction.CacheDirectory, "TestProvider/test/1.0/folder/file3.txt"));

            LibraryInstallationGoalState goalState = provider.GenerateGoalState(installState, _library);

            Assert.IsNotNull(goalState);
            Assert.AreEqual(1, goalState.InstalledFiles.Count);
            Assert.IsTrue(goalState.InstalledFiles.TryGetValue(expectedDestinationFile1, out string file1));
            Assert.AreEqual(expectedSourceFile1, file1);
        }

        [TestMethod]
        public void GenerateGoalState_NoFileMapping_NoFilesAtLibraryLevel()
        {
            ILibraryInstallationState installState = new LibraryInstallationState
            {
                Name = "test",
                Version = "1.0",
                ProviderId = "TestProvider",
                DestinationPath = "lib/test",
            };
            BaseProvider provider = new TestProvider(_hostInteraction, cacheService: null);
            string expectedDestinationFile1 = FileHelpers.NormalizePath(Path.Combine(provider.HostInteraction.WorkingDirectory, "lib/test/file1.txt"));
            string expectedSourceFile1 = FileHelpers.NormalizePath(Path.Combine(provider.HostInteraction.CacheDirectory, "TestProvider/test/1.0/file1.txt"));
            string expectedDestinationFile2 = FileHelpers.NormalizePath(Path.Combine(provider.HostInteraction.WorkingDirectory, "lib/test/file2.txt"));
            string expectedSourceFile2 = FileHelpers.NormalizePath(Path.Combine(provider.HostInteraction.CacheDirectory, "TestProvider/test/1.0/file2.txt"));
            string expectedDestinationFile3 = FileHelpers.NormalizePath(Path.Combine(provider.HostInteraction.WorkingDirectory, "lib/test/folder/file3.txt"));
            string expectedSourceFile3 = FileHelpers.NormalizePath(Path.Combine(provider.HostInteraction.CacheDirectory, "TestProvider/test/1.0/folder/file3.txt"));

            LibraryInstallationGoalState goalState = provider.GenerateGoalState(installState, _library);

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

            public override ILibraryCatalog GetCatalog()
            {
                throw new NotImplementedException();
            }

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
