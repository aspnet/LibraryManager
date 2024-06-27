// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Json;
using Microsoft.Web.LibraryManager.LibraryNaming;
using Microsoft.Web.LibraryManager.Mocks;
using Microsoft.Web.LibraryManager.Providers.Cdnjs;

namespace Microsoft.Web.LibraryManager.Test.Json
{
    [TestClass]
    public class LibraryStateToFileConverterTests
    {
        [TestInitialize]
        public void Setup()
        {
            string cacheFolder = Environment.ExpandEnvironmentVariables(@"%localappdata%\Microsoft\Library\");
            string projectFolder = Path.Combine(Path.GetTempPath(), "LibraryManager");
            var hostInteraction = new HostInteraction(projectFolder, cacheFolder);
            var dependencies = new Dependencies(hostInteraction, new CdnjsProviderFactory());
            IProvider provider = dependencies.GetProvider("cdnjs");
            LibraryIdToNameAndVersionConverter.Instance.Reinitialize(dependencies);
        }

        [TestMethod]
        public void ConvertToLibraryInstallationState_NullStateOnDisk()
        {
            LibraryStateToFileConverter converter = new LibraryStateToFileConverter("provider", "destination");

            ILibraryInstallationState result = converter.ConvertToLibraryInstallationState(null);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void ConvertToLibraryInstallationState_UseDefaultProviderAndDestination()
        {
            LibraryStateToFileConverter converter = new LibraryStateToFileConverter("defaultProvider", "defaultDestination");

            var stateOnDisk = new LibraryInstallationStateOnDisk
            {
                LibraryId = "libraryId",
            };

            ILibraryInstallationState result = converter.ConvertToLibraryInstallationState(stateOnDisk);

            Assert.AreEqual("defaultProvider", result.ProviderId);
            Assert.AreEqual("defaultDestination", result.DestinationPath);
        }

        [TestMethod]
        public void ConvertToLibraryInstallationState_OverrideProviderAndDestination()
        {
            LibraryStateToFileConverter converter = new LibraryStateToFileConverter("defaultProvider", "defaultDestination");

            var stateOnDisk = new LibraryInstallationStateOnDisk
            {
                LibraryId = "libraryId",
                ProviderId = "provider",
                DestinationPath = "destination",
            };

            ILibraryInstallationState result = converter.ConvertToLibraryInstallationState(stateOnDisk);

            Assert.AreEqual("provider", result.ProviderId);
            Assert.AreEqual("destination", result.DestinationPath);
        }

        [TestMethod]
        public void ConvertToLibraryInstallationState_ExpandTokensInDefaultDestination()
        {
            LibraryStateToFileConverter converter = new LibraryStateToFileConverter("defaultProvider", "lib/[Name]/[Version]");

            var stateOnDisk = new LibraryInstallationStateOnDisk
            {
                LibraryId = "testLibraryId@1.0",
                // it needs to be a provider that uses the versioned naming scheme
                ProviderId = "cdnjs",
            };

            ILibraryInstallationState result = converter.ConvertToLibraryInstallationState(stateOnDisk);

            Assert.AreEqual("lib/testLibraryId/1.0", result.DestinationPath);
        }

        [TestMethod]
        [DataRow("filesystem", "c:\\path\\to\\library")]
        [DataRow("filesystem", "/path/to/library")]
        [DataRow("cdnjs", "@scope/library@1.0.0")]
        public void ConvertToLibraryInstallationState_ExpandTokensInDefaultDestination_NamesWithSlashes(string provider, string libraryId)
        {
            LibraryStateToFileConverter converter = new LibraryStateToFileConverter("defaultProvider", "lib/[Name]");

            var stateOnDisk = new LibraryInstallationStateOnDisk
            {
                LibraryId = libraryId,
                ProviderId = provider,
            };

            ILibraryInstallationState result = converter.ConvertToLibraryInstallationState(stateOnDisk);

            Assert.AreEqual("lib/library", result.DestinationPath);
        }
    }
}
