// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.LibraryNaming;
using Microsoft.Web.LibraryManager.Providers.Cdnjs;
using Microsoft.Web.LibraryManager.Vsix.UI.Models;
using Newtonsoft.Json;

namespace Microsoft.Web.LibraryManager.Vsix.Test.UI.Models
{
    [TestClass]
    public class InstallDialogViewModelTest
    {
        // these settings should be kept in sync with those used in GetLibraryTextToBeInserted to ensure that the expectedObj is
        // serialized the same as resultString.
        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
        };
        private Manifest _manifest;

        [TestInitialize]
        public void Setup()
        {
            var dependencies = new Mocks.Dependencies(new Mocks.HostInteraction(), new CdnjsProviderFactory());
            LibraryIdToNameAndVersionConverter.Instance.Reinitialize(dependencies);
            _manifest = new Manifest(dependencies);
        }

        [TestMethod]
        public void GetLibraryTextToBeInserted_BasicProperties()
        {
            var installState = new LibraryInstallationState
            {
                ProviderId = "cdnjs",
                Name = "jquery",
                Version = "3.3.1",
            };

            string resultString = InstallDialogViewModel.GetLibraryTextToBeInserted(installState, _manifest);

            var expectedObj = new
            {
                provider = "cdnjs",
                library = "jquery@3.3.1",
            };
            string expected = JsonConvert.SerializeObject(expectedObj, _jsonSettings);

            Assert.AreEqual(expected, resultString);
        }
    }
}
