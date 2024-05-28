// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Test
{
    [TestClass]
    public class FileHelpersTest
    {
        [DataTestMethod]
        [DataRow("C:\\dir\\file.js", "C:\\dir\\", true)]
        [DataRow("C:\\dir\\file1.js", "C:\\dir", true)]
        [DataRow("C:\\dir\\", "C:\\dir\\", false)]
        [DataRow("/abc/def/ghi", "\\abc\\def", true)]
        [DataRow("abc/def", "abc", true)]
        [DataRow("abcdef", "abc", false)]
        public void UnderRootDirectory(string file, string directory, bool expectedResult)
        {
            Assert.AreEqual(expectedResult, FileHelpers.IsUnderRootDirectory(file, directory));
        }
    }
}
