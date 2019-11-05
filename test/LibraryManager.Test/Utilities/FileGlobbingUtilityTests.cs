using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Utilities;

namespace Microsoft.Web.LibraryManager.Test.Utilities
{
    [TestClass]
    public class FileGlobbingUtilityTests
    {
        [TestMethod]
        public void GetMatchedFiles_NoGlobsSpecified_ReturnsInputList()
        {
            string[] libraryFiles = new[]
            {
                "file.css",
                "folder/file.css",
            };
            string[] fileSpec = new[]
            {
                "file.css",
            };

            IEnumerable<string> result = FileGlobbingUtility.ExpandFileGlobs(fileSpec, libraryFiles);

            CollectionAssert.AreEqual(fileSpec, result.ToList());
        }

        [TestMethod]
        public void GetMatchedFiles_NoGlobsSpecifiedAndNoMatches_ReturnsInputList()
        {
            string[] libraryFiles = new[]
            {
                "folder/file.css",
            };
            string[] fileSpec = new[]
            {
                "file.css",
            };

            IEnumerable<string> result = FileGlobbingUtility.ExpandFileGlobs(fileSpec, libraryFiles);

            CollectionAssert.AreEqual(fileSpec, result.ToList());
        }

        [TestMethod]
        public void GetMatchedFiles_ContainsGlobMatchingZeroFiles_EmptyList()
        {
            string[] libraryFiles = new[]
            {
                "file.js",
            };
            string[] fileSpec = new[]
            {
                "*.css",
            };

            IEnumerable<string> result = FileGlobbingUtility.ExpandFileGlobs(fileSpec, libraryFiles);

            CollectionAssert.AreEqual(Array.Empty<string>(), result.ToList());
        }

        [TestMethod]
        public void GetMatchedFiles_ContainsGlobMatchingMultipleFiles_ReturnsAllMatches()
        {
            string[] libraryFiles = new[]
            {
                "a.css",
                "b.css",
            };
            string[] fileSpec = new[]
            {
                "*.css",
            };

            IEnumerable<string> result = FileGlobbingUtility.ExpandFileGlobs(fileSpec, libraryFiles);

            CollectionAssert.AreEqual(libraryFiles, result.ToList());
        }

        [TestMethod]
        public void GetMatchedFiles_ContainsGlobsUsingGlobStar_ReturnsMatches()
        {
            string[] libraryFiles = new[] {
                "notMatch/foo/file.css",
                "match/foo/a.css",
                "match/bar/baz/b.css",
            };
            string[] fileSpec = new[] {
                "match/**/*.css",
            };

            IEnumerable<string> result = FileGlobbingUtility.ExpandFileGlobs(fileSpec, libraryFiles);

            string[] expected = new[] {
                "match/foo/a.css",
                "match/bar/baz/b.css",
            };
            CollectionAssert.AreEqual(expected, result.ToList());
        }

        [TestMethod]
        public void GetMatchedFiles_ContainsGlobsWithExclusion_ReturnsRemovesFilesMatchingExclusion()
        {
            string[] libraryFiles = new[] {
                "notMatch/foo/file.css",
                "match/foo/a.css",
                "match/bar/baz/b.css",
            };
            string[] fileSpec = new[] {
                "match/**/*.css",
                "!**/b.css",
            };

            IEnumerable<string> result = FileGlobbingUtility.ExpandFileGlobs(fileSpec, libraryFiles);

            string[] expected = new[] {
                "match/foo/a.css",
            };
            CollectionAssert.AreEqual(expected, result.ToList());
        }

        [TestMethod]
        public void GetMatchedFiles_ContainsGlobsWithExclusion_ProcessedInOrderSeen()
        {
            string[] libraryFiles = new[] {
                "match/foo/a.css",
                "match/bar/baz/b.css",
                "other/foo/b.css",
            };
            string[] fileSpec = new[] {
                "match/**/*.css",
                "!**/b.css",
                "other/**/*.css"
            };

            IEnumerable<string> result = FileGlobbingUtility.ExpandFileGlobs(fileSpec, libraryFiles);

            string[] expected = new[] {
                "match/foo/a.css",
                "other/foo/b.css",
            };
            CollectionAssert.AreEqual(expected, result.ToList());
        }
    }
}
