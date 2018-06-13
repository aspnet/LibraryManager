using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Mocks;
using Microsoft.Web.LibraryManager.Providers.Unpkg;

namespace Microsoft.Web.LibraryManager.Test.Providers.Unpkg
{
    [TestClass]
    public class UnpkgProviderTest
    {
        private string _projectFolder;
        private IProvider _provider;
        private ILibraryCatalog _catalog;

        [TestInitialize]
        public void Setup()
        {
            _projectFolder = Path.Combine(Path.GetTempPath(), "LibraryManager");

            var hostInteraction = new HostInteraction(_projectFolder, "");
            var dependencies = new Dependencies(hostInteraction, new UnpkgProviderFactory());
            _provider = dependencies.GetProvider("unpkg");
            _catalog = new UnpkgCatalog((UnpkgProvider)_provider);
        }


        [TestMethod]
        public void GetSuggestedDestination()
        {

            Assert.AreEqual(string.Empty, _provider.GetSuggestedDestination(null));

            var library = new UnpkgLibrary()
            {
                Name = "jquery",
                Version = "3.3.1",
                Files = null
            };

            Assert.AreEqual(library.Name, _provider.GetSuggestedDestination(library));

            library.Name = @"@angular/cli";

            Assert.AreEqual("@angular/cli", _provider.GetSuggestedDestination(library));
        }
    }
}
