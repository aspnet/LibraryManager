// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Test
{
    [TestClass]
    public class InvalidLibraryExceptionTest
    {
        [TestMethod]
        public void Constructor()
        {
            var ex = new InvalidLibraryException("123", "abc");

            Assert.AreEqual("123", ex.LibraryId);
            Assert.AreEqual("abc", ex.ProviderId);
            Assert.IsNotNull(ex.Message);
        }
    }
}
