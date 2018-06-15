// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Providers.Shared;

namespace Microsoft.Web.LibraryManager.Test
{
    [TestClass]
    public class ProvidersCommonUtilsTest
    {
        [DataRow("cdnjs", "name")]
        [DataRow("cdnjs", "name@")]
        [DataRow("cdnjs", "@version")]
        [DataRow("cdnjs", "Name@version ")]
        [DataRow("cdnjs", " Name@version")]
        [DataRow("cdnjs", "Name @version")]
        [DataRow("cdnjs", "Name@version ")]
        [DataRow("unpkg", "name")]
        [DataRow("unpkg", "name@")]
        [DataRow("unpkg", "@version")]
        [DataRow("unpkg", "Name@version ")]
        [DataRow("unpkg", " Name@version")]
        [DataRow("unpkg", "Name @version")]
        [DataRow("unpkg", "Name@version ")]
        [TestMethod]
        [ExpectedException(typeof(InvalidLibraryException))]
        public void GetLibraryIdentifier_ThrowsForNoDelimiter(string providerId, string libraryId)
        {
            ILibrary library = ProvidersCommonUtils.GetLibraryIdentifier(providerId, libraryId);
        }

        [TestMethod]
        public void GetLibraryIdentifier_VerifyExceptionMessage()
        {
            string expectedMessage = "The \"\" library could not be resolved by the \"cdnjs\" provider\r\nName and Version of a library are required";
            string actualMessage = null;

            try
            {
                ILibrary library = ProvidersCommonUtils.GetLibraryIdentifier("cdnjs", "");
            }
            catch (InvalidLibraryException ex)
            {
                actualMessage = ex.Message;
            }

            Assert.AreEqual(expectedMessage, actualMessage);
        }
    }
}
