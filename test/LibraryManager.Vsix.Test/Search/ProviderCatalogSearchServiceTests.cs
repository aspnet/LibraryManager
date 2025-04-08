// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Vsix.Search;

namespace Microsoft.Web.LibraryManager.Vsix.Test.Search
{
    using Mocks = LibraryManager.Mocks;

    [TestClass]
    public class ProviderCatalogSearchServiceTests
    {
        private readonly IProvider _testProvider = new Mocks.Provider(new Mocks.HostInteraction())
        {
            Catalog = new Mocks.LibraryCatalog()
                .AddLibrary(new Mocks.Library { Name = "aardvark", Version = "2.0" })
                .AddLibrary(new Mocks.Library { Name = "anteater", Version = "1.0" })
                .AddLibrary(new Mocks.Library { Name = "platypus", Version = "1.2" }),
        };

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ctor_NullLookup_Throws()
        {
            new ProviderCatalogSearchService(null);
        }

        [TestMethod]
        public async Task PerformSearch_LookupYieldsNullProvider_ReturnsEmptyCompletionSet()
        {
            var testObj = new ProviderCatalogSearchService(() => null);

            CompletionSet result = await testObj.PerformSearch("", 0);

            Assert.AreEqual(default(CompletionSet), result);
        }

        [TestMethod]
        public async Task PerformSearch_ProviderIsNotNull_ReturnsProviderResults()
        {
            var testObj = new ProviderCatalogSearchService(() => _testProvider);

            CompletionSet result = await testObj.PerformSearch("", 0);

            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Completions.Count());
        }
    }
}
