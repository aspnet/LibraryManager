// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
