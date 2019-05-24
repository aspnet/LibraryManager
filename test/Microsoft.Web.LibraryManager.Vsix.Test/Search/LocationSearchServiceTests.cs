// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Vsix.Search;

namespace Microsoft.Web.LibraryManager.Vsix.Test.Search
{
    using Mocks = LibraryManager.Mocks;

    [TestClass]
    public class LocationSearchServiceTests
    {
        private static string TestDirectoryRoot;

        [ClassInitialize]
        public static void CreateTestDirectories(TestContext testContext)
        {
            TestDirectoryRoot = Path.Combine(testContext.DeploymentDirectory, nameof(LocationSearchServiceTests));

            Directory.CreateDirectory(Path.Combine(TestDirectoryRoot, "RootFolder1"));
            Directory.CreateDirectory(Path.Combine(TestDirectoryRoot, "RootFolder2"));
            Directory.CreateDirectory(Path.Combine(TestDirectoryRoot, "RootFolder2", "SubFolder"));
            Directory.CreateDirectory(Path.Combine(TestDirectoryRoot, "DifferentlyNamedFolder"));
            File.WriteAllText(Path.Combine(TestDirectoryRoot, "RootFile.txt"), "");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ctor_NullHost_Throws()
        {
            new LocationSearchService(null);
        }

        [TestMethod]
        public async Task PerformSearch_NullSearchText_TreatAsEmpty()
        {
            var host = new Mocks.HostInteraction(TestDirectoryRoot, null);
            var testObj = new LocationSearchService(host);
            string searchString = null;

            CompletionSet result = await testObj.PerformSearch(searchString, 0);

            Assert.AreEqual(3, result.Completions.Count());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public async Task PerformSearch_NegativeCaretPosition_Throws()
        {
            var host = new Mocks.HostInteraction(TestDirectoryRoot, null);
            var testObj = new LocationSearchService(host);

            _ = await testObj.PerformSearch("", -2);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public async Task PerformSearch_CaretPositionBeyondSearchTextLength_Throws()
        {
            var host = new Mocks.HostInteraction(TestDirectoryRoot, null);
            var testObj = new LocationSearchService(host);

            _ = await testObj.PerformSearch("test", 5);
        }


        [TestMethod]
        public async Task PerformSearch_EmptySearchText_ReturnsContentsOfWorkingDirectory()
        {
            var host = new Mocks.HostInteraction(TestDirectoryRoot, null);
            var testObj = new LocationSearchService(host);
            string searchString = "";

            CompletionSet result = await testObj.PerformSearch(searchString, searchString.Length);

            Assert.AreEqual(3, result.Completions.Count());
            Assert.IsTrue(result.Completions.Any(c => c.InsertionText == "RootFolder1/"));
            Assert.IsTrue(result.Completions.Any(c => c.InsertionText == "RootFolder2/"));
            Assert.IsTrue(result.Completions.Any(c => c.InsertionText == "DifferentlyNamedFolder/"));
        }

        [TestMethod]
        public async Task PerformSearch_SearchTextDoesNotContainSeparator_FilterSearchResults()
        {
            var host = new Mocks.HostInteraction(TestDirectoryRoot, null);
            var testObj = new LocationSearchService(host);
            string searchString = "Root";

            CompletionSet result = await testObj.PerformSearch(searchString, searchString.Length);

            Assert.AreEqual(2, result.Completions.Count());
            Assert.IsTrue(result.Completions.Any(c => c.InsertionText == "RootFolder1/"));
            Assert.IsTrue(result.Completions.Any(c => c.InsertionText == "RootFolder2/"));
        }

        [TestMethod]
        public async Task PerformSearch_SearchTextContainsSeparator_ReturnSubdirectoriesOnly()
        {
            var host = new Mocks.HostInteraction(TestDirectoryRoot, null);
            var testObj = new LocationSearchService(host);
            string searchString = "RootFolder2/";

            CompletionSet result = await testObj.PerformSearch(searchString, searchString.Length);

            Assert.AreEqual(1, result.Completions.Count());
            Assert.IsTrue(result.Completions.Any(c => c.InsertionText == "RootFolder2/SubFolder/"));
        }
    }
}
