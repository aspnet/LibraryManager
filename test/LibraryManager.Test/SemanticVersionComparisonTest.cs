// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Providers.Unpkg;

namespace Microsoft.Web.LibraryManager.Test
{
    [TestClass]
    public class SemanticVersionComparisonTest
    {
        [DataTestMethod]
        [DataRow(null, "1.0.0", "1.0.0")]
        [DataRow("1.0.0", null, "1.0.0")]
        [DataRow(null, null, null)]
        [DataRow("2.0.0", "1.10.1", "2.0.0")]
        [DataRow("3.0.0", "3.0.0-beta1", "3.0.0")]
        public void CompareSemanticVersion(string selfVersion, string otherVersion, string laterVersion)
        {
            List<SemanticVersion> versions = new List<SemanticVersion>();

            versions.Add(SemanticVersion.Parse(selfVersion));
            versions.Add(SemanticVersion.Parse(otherVersion));
            versions.Sort();
            
            Assert.AreEqual(versions[1].OriginalText, laterVersion);
        }
    }
}
