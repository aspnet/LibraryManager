// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
