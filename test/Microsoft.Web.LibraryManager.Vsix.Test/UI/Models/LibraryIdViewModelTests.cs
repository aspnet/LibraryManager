// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Vsix.Search;
using Microsoft.Web.LibraryManager.Vsix.UI.Models;

namespace Microsoft.Web.LibraryManager.Vsix.Test.UI.Models
{
    using LibraryManager.Mocks;

    [TestClass]
    public class LibraryIdViewModelTests
    {
        private readonly ILibraryCatalog _testCatalog = new LibraryCatalog()
            .AddLibrary(new Library { Name = "test", Version = "1.0" })
            .AddLibrary(new Library { Name = "test", Version = "1.2" })
            .AddLibrary(new Library { Name = "test", Version = "2.0" });

        private IProvider GetTestProvider()
        {
            IProvider testProvider = new Provider(new HostInteraction())
            {
                Catalog = _testCatalog
            };

            return testProvider;
        }

        private ISearchService GetTestSearchService()
        {
            IProvider provider = GetTestProvider();
            return new ProviderCatalogSearchService(() => provider);
        }

        [TestMethod]
        public async Task FilterCompletions_SearchTextDoesNotContainAtSign_ReturnAllMatchingCompletions()
        {
            var testObj = new LibraryIdViewModel(GetTestSearchService(), "test");

            CompletionSet result = await testObj.GetCompletionSetAsync(caretIndex: 0);

            Assert.AreEqual(3, result.Completions.Count());
        }

        [TestMethod]
        public async Task FilterCompletions_SearchTextEndsWithAt_ReturnAllMatchingCompletions()
        {
            var testObj = new LibraryIdViewModel(GetTestSearchService(), "test@");

            CompletionSet result = await testObj.GetCompletionSetAsync(caretIndex: 0);

            // the version segment is "", which matches all versions
            Assert.AreEqual(3, result.Completions.Count());
        }

        [TestMethod]
        public async Task FilterCompletions_SearchTextEndsWithAfterAt_ReturnFilteredCompletions()
        {
            var testObj = new LibraryIdViewModel(GetTestSearchService(), "test@2");

            CompletionSet result = await testObj.GetCompletionSetAsync(caretIndex: 0);

            // This will match both 1.2 and 2.0
            Assert.AreEqual(2, result.Completions.Count());
        }

        [TestMethod]
        public async Task GetRecommendedSelectedCompletionAsync_DoesNotContainAt_ReturnsFirstItem()
        {
            var testObj = new LibraryIdViewModel(GetTestSearchService(), "test");
            var completionSet = new CompletionSet
            {
                Start = 0,
                Length = 4,
                Completions = new[] {
                    new CompletionItem { DisplayText = "test@1.2" },
                    new CompletionItem { DisplayText = "test@2.1" },
                },
            };

            CompletionItem result = await testObj.GetRecommendedSelectedCompletionAsync(completionSet, null);

            Assert.AreEqual("test@1.2", result.DisplayText);
        }

        [TestMethod]
        public async Task GetRecommendedSelectedCompletionAsync_SearchTextDoesContainAt_ReturnsItemThatStartsWithPrefix()
        {
            var testObj = new LibraryIdViewModel(GetTestSearchService(), "test@2");
            var completionSet = new CompletionSet
            {
                Start = 0,
                Length = 4,
                Completions = new[] {
                    new CompletionItem { DisplayText = "1.2" },
                    new CompletionItem { DisplayText = "2.1" },
                },
            };

            CompletionItem result = await testObj.GetRecommendedSelectedCompletionAsync(completionSet, null);

            Assert.AreEqual("2.1", result.DisplayText);
        }

        [TestMethod]
        public async Task GetRecommendedSelectedCompletionAsync_SearchTextDoesContainAt_NonMatching_ReturnsFirstItem()
        {
            var testObj = new LibraryIdViewModel(GetTestSearchService(), "test@3");
            var completionSet = new CompletionSet
            {
                Start = 0,
                Length = 4,
                Completions = new[] {
                    new CompletionItem { DisplayText = "1.2" },
                    new CompletionItem { DisplayText = "2.1" },
                },
            };

            CompletionItem result = await testObj.GetRecommendedSelectedCompletionAsync(completionSet, null);

            Assert.AreEqual("1.2", result.DisplayText);
        }
    }
}
