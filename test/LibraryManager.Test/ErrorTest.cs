// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Mocks;

namespace Microsoft.Web.LibraryManager.Test
{
    [TestClass]
    public class ErrorTest
    {
        [TestMethod]
        public void Constructor()
        {
            var error = new Mocks.Error("123", "abc");

            Assert.AreEqual("123", error.Code);
            Assert.AreEqual("abc", error.Message);
        }

        [TestMethod]
        public void Predefined()
        {
            TestError(PredefinedErrors.UnknownException(), "LIB000");
            TestError(PredefinedErrors.ProviderUnknown("_prov_"), "LIB001", "_prov_");
            TestError(PredefinedErrors.UnableToResolveSource("_libid_", "_prov_"), "LIB002", "_libid_", "_prov_");
            TestError(PredefinedErrors.CouldNotWriteFile("file.js"), "LIB003", "file.js");
            TestError(PredefinedErrors.ManifestMalformed(), "LIB004");
            TestError(PredefinedErrors.PathIsUndefined(), "LIB005");
            TestError(PredefinedErrors.LibraryIdIsUndefined(), "LIB006");
            TestError(PredefinedErrors.ProviderIsUndefined(), "LIB007");
        }

        private void TestError(IError error, string code, params string[] pieces)
        {
            Assert.AreEqual(code, error.Code);

            foreach (string piece in pieces)
            {
                Assert.IsTrue(error.Message.Contains(piece));
            }
        }
    }
}
