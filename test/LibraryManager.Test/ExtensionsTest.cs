// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Helpers;
using Microsoft.Web.LibraryManager.Mocks;

namespace Microsoft.Web.LibraryManager.Test
{
    [TestClass]
    public class ExtensionsTest
    {
        [TestMethod]
        public void TestGetInvalidFiles()
        {
            var files = new Dictionary<string, bool>
            {
                ["abc.js"] = true,
                ["xyz.js"] = false
            };

            ILibrary library = new Library()
            {
                Files = files,
                Name = "DummyLibrary",
                ProviderId = "DummyProvider"
            };

            IReadOnlyList<string> invalidFiles = Extensions.GetInvalidFiles(library, null);

            Assert.AreEqual(0, invalidFiles.Count);

            var filesToCheck = new List<string>()
            {
                "abc.js",
                "xyz.js"
            };

            invalidFiles = Extensions.GetInvalidFiles(library, filesToCheck);

            Assert.AreEqual(0, invalidFiles.Count);

            filesToCheck.Add("def");
            invalidFiles = Extensions.GetInvalidFiles(library, filesToCheck);

            Assert.AreEqual(1, invalidFiles.Count);

            Assert.AreEqual("def", invalidFiles[0]);
        }
    }
}
