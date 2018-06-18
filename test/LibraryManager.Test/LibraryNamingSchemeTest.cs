// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Helpers;

namespace Microsoft.Web.LibraryManager.Test
{
    [TestClass]
    public class LibraryNamingSchemeTest
    {
        [DataTestMethod]
        [DataRow("jquery@3.3.1", "jquery", "3.3.1")]
        [DataRow("@angular/cli@1.0.0", "@angular/cli","1.0.0")]
        [DataRow("My@Random@Library@1.0.0-preview3-final", "My@Random@Library", "1.0.0-preview3-final")]
        [DataRow("@MyLibraryWithoutVersion", "@MyLibraryWithoutVersion", "")]
        [DataRow("Library@Version", "Library", "Version")]
        [DataRow(null, "", "")]
        [DataRow("", "", "")]
        public void GetLibraryNameAndVersion(string libraryId, string expectedName, string expectedVersion)
        {
            LibraryNamingScheme.Instance.GetLibraryNameAndVersion(libraryId, out string name, out string version);

            Assert.AreEqual(expectedName, name);
            Assert.AreEqual(expectedVersion, version);
        }

        [DataTestMethod]
        [DataRow("jquery", "3.3.1", "jquery@3.3.1")]
        [DataRow("@angular/cli","1.0.0", "@angular/cli@1.0.0")]
        [DataRow("My@Random@Library", "1.0.0-preview3-final", "My@Random@Library@1.0.0-preview3-final")]
        [DataRow("@MyLibraryWithoutVersion", "", "@MyLibraryWithoutVersion")]
        [DataRow("Library", "Version", "Library@Version")]
        [DataRow("", "", "")]
        public void GetLibraryId(string name, string version, string expectedLibraryId)
        {
            string libraryId = LibraryNamingScheme.Instance.GetLibraryId(name, version);

            Assert.AreEqual(expectedLibraryId, libraryId);
        }
    }
}