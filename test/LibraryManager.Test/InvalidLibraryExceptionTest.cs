// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
