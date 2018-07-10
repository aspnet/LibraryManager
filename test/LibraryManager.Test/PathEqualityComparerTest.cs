// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

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
    }
}
