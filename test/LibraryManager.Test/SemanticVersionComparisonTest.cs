// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Providers.Unpkg;

namespace Microsoft.Web.LibraryManager.Test
{
    [TestClass]
    public class SemanticVersionComparisonTest
    {
        [DataTestMethod]
        [DataRow(null, "1.0.0", 1)]
        [DataRow("1.0.0", null, -1)]
        [DataRow(null, null, 0)]
        [DataRow("2.0.0", "1.10.1", -1)]
        [DataRow("3.0.0", "3.0.0-beta1", -1)]
        public void CompareSemanticVersion(string selfVersion, string otherVersion, int expectedResult)
        {
            SemanticVersion selfSemVersion = SemanticVersion.Parse(selfVersion);
            SemanticVersion otherSemVersion = SemanticVersion.Parse(otherVersion);

            int actualResult = CompletionUtility.CompareSemanticVersion(selfSemVersion, otherSemVersion);

            Assert.AreEqual(expectedResult, actualResult);
        }
    }
}
