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
        private ISearchService GetTestSearchService(ILibraryCatalog libraryCatalog)
        {
            IProvider testProvider = new Provider(new HostInteraction()) { Catalog = libraryCatalog };
            return new ProviderCatalogSearchService(() => testProvider);
        }

        private ILibraryCatalog CreateLibraryCatalogWithUnscopedLibrary()
        {
            return new LibraryCatalog()
                .AddLibrary(new Library { Name = "test", Version = "1.0" })
                .AddLibrary(new Library { Name = "test", Version = "1.2" })
                .AddLibrary(new Library { Name = "test", Version = "2.0" });
        }

        [TestMethod]
        public async Task FilterCompletions_SearchTextDoesNotContainAtSign_ReturnAllMatchingCompletions()
        {
            ILibraryCatalog testCatalog = new LibraryCatalog()
                .AddLibrary(new Library { Name = "test", Version = "1.0" })
                .AddLibrary(new Library { Name = "@types/test", Version = "1.2" });

            var testObj = new LibraryIdViewModel(GetTestSearchService(testCatalog), "test");

            CompletionSet result = await testObj.GetCompletionSetAsync(caretIndex: 3);

            Assert.AreEqual(1, result.Completions.Count());
        }

        [TestMethod]
        public async Task FilterCompletions_SearchTextEndsWithAt_ReturnAllMatchingCompletions()
        {
            ILibraryCatalog testCatalog = new LibraryCatalog()
                .AddLibrary(new Library { Name = "test", Version = "1.0" })
                .AddLibrary(new Library { Name = "test", Version = "1.2" })
                .AddLibrary(new Library { Name = "@types/test", Version = "1.2" });

            var testObj = new LibraryIdViewModel(GetTestSearchService(testCatalog), "test@");

            CompletionSet result = await testObj.GetCompletionSetAsync(caretIndex: 5);

            // the version segment is "", which matches all versions
            Assert.AreEqual(2, result.Completions.Count());
        }

        [TestMethod]
        public async Task FilterCompletions_ScopedPackageWithoutTrailingAt_ReturnAllMatchingCompletions()
        {
            ILibraryCatalog testCatalog = new LibraryCatalog()
                .AddLibrary(new Library { Name = "test", Version = "1.0" })
                .AddLibrary(new Library { Name = "@types/test", Version = "1.2" });

            var testObj = new LibraryIdViewModel(GetTestSearchService(testCatalog), "@types/test");

            CompletionSet result = await testObj.GetCompletionSetAsync(caretIndex: 11);

            Assert.AreEqual(1, result.Completions.Count());
        }

        [TestMethod]
        public async Task FilterCompletions_ScopedPackageEndsWithAt_ReturnAllMatchingCompletions()
        {
            ILibraryCatalog testCatalog = new LibraryCatalog()
                .AddLibrary(new Library { Name = "@types/test", Version = "1.0" })
                .AddLibrary(new Library { Name = "@types/test", Version = "1.2" })
                .AddLibrary(new Library { Name = "test", Version = "1.2" });

            var testObj = new LibraryIdViewModel(GetTestSearchService(testCatalog), "@types/test@");

            CompletionSet result = await testObj.GetCompletionSetAsync(caretIndex: 12);

            // the version segment is "", which matches all versions
            Assert.AreEqual(2, result.Completions.Count());
        }

        [TestMethod]
        public async Task FilterCompletions_ScopedPackageEndsWithAfterAt_ReturnAllMatchingCompletions()
        {
            ILibraryCatalog testCatalog = new LibraryCatalog()
                .AddLibrary(new Library { Name = "@types/test", Version = "1.0" })
                .AddLibrary(new Library { Name = "@types/test", Version = "1.2" })
                .AddLibrary(new Library { Name = "@types/test", Version = "2.0" })
                .AddLibrary(new Library { Name = "test", Version = "1.2" });

            var testObj = new LibraryIdViewModel(GetTestSearchService(testCatalog), "@types/test@2");

            CompletionSet result = await testObj.GetCompletionSetAsync(caretIndex: 12);

            Assert.AreEqual(2, result.Completions.Count());
        }

        [TestMethod]
        public async Task FilterCompletions_SearchTextEndsWithAfterAt_ReturnFilteredCompletions()
        {
            ILibraryCatalog testCatalog = new LibraryCatalog()
                .AddLibrary(new Library { Name = "test", Version = "1.0" })
                .AddLibrary(new Library { Name = "test", Version = "1.2" })
                .AddLibrary(new Library { Name = "test", Version = "2.0" })
                .AddLibrary(new Library { Name = "@types/test", Version = "1.2" });

            var testObj = new LibraryIdViewModel(GetTestSearchService(testCatalog), "test@2");

            CompletionSet result = await testObj.GetCompletionSetAsync(caretIndex: 5);

            // This will match both 1.2 and 2.0
            Assert.AreEqual(2, result.Completions.Count());
        }

        [TestMethod]
        public async Task GetRecommendedSelectedCompletionAsync_DoesNotContainAt_ReturnsFirstItem()
        {
            ILibraryCatalog testCatalog = CreateLibraryCatalogWithUnscopedLibrary();

            var testObj = new LibraryIdViewModel(GetTestSearchService(testCatalog), "test");
            CompletionItem[] completions = new[] {
                new CompletionItem { DisplayText = "test@1.2" },
                new CompletionItem { DisplayText = "test@2.1" },
            };

            CompletionItem result = await testObj.GetRecommendedSelectedCompletionAsync(completions, null);

            Assert.AreEqual("test@1.2", result.DisplayText);
        }

        [TestMethod]
        public async Task GetRecommendedSelectedCompletionAsync_SearchTextDoesContainAt_ReturnsItemThatStartsWithPrefix()
        {
            ILibraryCatalog testCatalog = CreateLibraryCatalogWithUnscopedLibrary();

            var testObj = new LibraryIdViewModel(GetTestSearchService(testCatalog), "test@2");
            CompletionItem[] completions = new[] {
                new CompletionItem { DisplayText = "1.2" },
                new CompletionItem { DisplayText = "2.1" },
            };

            CompletionItem result = await testObj.GetRecommendedSelectedCompletionAsync(completions, null);

            Assert.AreEqual("2.1", result.DisplayText);
        }

        [TestMethod]
        public async Task GetRecommendedSelectedCompletionAsync_SearchTextDoesContainAt_NonMatching_ReturnsFirstItem()
        {
            ILibraryCatalog testCatalog = CreateLibraryCatalogWithUnscopedLibrary();

            var testObj = new LibraryIdViewModel(GetTestSearchService(testCatalog), "test@3");
            CompletionItem[] completions = new[] {
                new CompletionItem { DisplayText = "1.2" },
                new CompletionItem { DisplayText = "2.1" },
            };

            CompletionItem result = await testObj.GetRecommendedSelectedCompletionAsync(completions, null);

            Assert.AreEqual("1.2", result.DisplayText);
        }
    }
}
