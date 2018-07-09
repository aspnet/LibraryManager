// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Test
{
    [TestClass]
    public class PathEqualityComparerTest
    {
        [DataTestMethod]
        [DataRow("C:\\dir", "C:\\dir\\", true)]
        [DataRow("C:\\dir", "C:\\dir\\..\\dir", true)]
        [DataRow("C:\\dir", "", false)]
        [DataRow("", "", true)]
        [DataRow(null, "", false)]
        [DataRow(null, null, true)]
        [DataRow("\\abc\\def", "abc\\def\\", true)]
        [DataRow("abc\\def", "abc\\def\\\\", true)]
        [DataRow("abc/def", "abc\\def\\\\", true)]
        public void Compare(string path1, string path2, bool expectedResult)
        {
            Assert.AreEqual(expectedResult, RelativePathEqualityComparer.Instance.Equals(path1, path2));
        }

        [DataTestMethod]
        [DataRow("C:\\dir\\file.js", "C:\\dir\\", true)]
        [DataRow("C:\\dir\\file1.js", "C:\\dir", true)]
        [DataRow("C:\\dir\\", "C:\\dir\\", false)]
        [DataRow("/abc/def/ghi", "\\abc\\def", true)]
        public void UnderRootDirectory(string path1, string path2, bool expectedResult)
        {
            Assert.AreEqual(expectedResult, FileHelpers.IsUnderRootDirectory(path1, path2));
        }
    }
}
