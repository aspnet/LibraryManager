// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.LibraryNaming;

namespace Microsoft.Web.LibraryManager.Test
{
    [TestClass]
    public class SimpleLibraryNamingSchemeTest
    {
        [DataTestMethod]
        [DataRow("jquery@3.3.1", "jquery@3.3.1", "")]
        [DataRow("@angular/cli", "@angular/cli", "")]
        [DataRow("My@Random@Library@", "My@Random@Library@", "")]
        [DataRow("@MyLibraryWithoutVersion", "@MyLibraryWithoutVersion", "")]
        [DataRow("Partial@", "Partial@", "")]
        [DataRow(null, "", "")]
        [DataRow("", "", "")]
        public void GetLibraryNameAndVersion(string libraryId, string expectedName, string expectedVersion)
        {
            var namingScheme = new SimpleLibraryNamingScheme();
            (string name, string version) = namingScheme.GetLibraryNameAndVersion(libraryId);

            Assert.AreEqual(expectedName, name);
            Assert.AreEqual(expectedVersion, version);
        }

        [DataTestMethod]
        [DataRow("jquery", "3.3.1", "jquery")]
        [DataRow("@angular/cli", "1.0.0", "@angular/cli")]
        [DataRow("My@Random@Library", "1.0.0-preview3-final", "My@Random@Library")]
        [DataRow("@MyLibraryWithoutVersion", "", "@MyLibraryWithoutVersion")]
        [DataRow("", "", "")]
        [DataRow(null, null, "")]
        [DataRow("Partial", "", "Partial")]
        [DataRow("Partial@", "", "Partial@")]
        public void GetLibraryId(string name, string version, string expectedLibraryId)
        {
            var namingScheme = new SimpleLibraryNamingScheme();
            string libraryId = namingScheme.GetLibraryId(name, version);

            Assert.AreEqual(expectedLibraryId, libraryId);
        }

        [TestMethod]
        [DataRow(null, false)]
        [DataRow("", false)]
        [DataRow("foobarbaz", true)]
        [DataRow(":@#/\\|", true)]
        [DataRow(" \t\r\n", true)]
        public void IsValidLibraryId(string libraryId, bool expected)
        {
            var namingScheme = new SimpleLibraryNamingScheme();

            bool result = namingScheme.IsValidLibraryId(libraryId);

            Assert.AreEqual(expected, result);
        }
    }
}
