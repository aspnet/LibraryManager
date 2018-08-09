// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Test
{
    [TestClass]
    public class LibraryOperationResultTest
    {
        [TestMethod]
        public void Constructor()
        {
            Mocks.LibraryInstallationState state = GetState();

            var ctor1 = new LibraryOperationResult(state);
            Assert.AreEqual(state, ctor1.InstallationState);
            Assert.AreEqual(0, ctor1.Errors.Count);
            Assert.IsTrue(ctor1.Success);
            Assert.IsFalse(ctor1.Cancelled);

            var ctor2 = new LibraryOperationResult(state, PredefinedErrors.ManifestMalformed());
            Assert.AreEqual(state, ctor2.InstallationState);
            Assert.AreEqual(1, ctor2.Errors.Count);
            Assert.IsFalse(ctor2.Success);
            Assert.IsFalse(ctor2.Cancelled);
        }

        [TestMethod]
        public void FromSuccess()
        {
            Mocks.LibraryInstallationState state = GetState();
            var result = LibraryOperationResult.FromSuccess(state);

            Assert.AreEqual(state, result.InstallationState);
            Assert.AreEqual(0, result.Errors.Count);
            Assert.IsTrue(result.Success);
            Assert.IsFalse(result.Cancelled);
        }

        [TestMethod]
        public void FromCancelled()
        {
            Mocks.LibraryInstallationState state = GetState();
            var result = LibraryOperationResult.FromCancelled(state);

            Assert.AreEqual(state, result.InstallationState);
            Assert.AreEqual(0, result.Errors.Count);
            Assert.IsFalse(result.Success);
            Assert.IsTrue(result.Cancelled);
        }

        private static Mocks.LibraryInstallationState GetState()
        {
            return new Mocks.LibraryInstallationState
            {
                ProviderId = "_prov_",
                Name = "_lib_",
                DestinationPath = "_path_",
                Files = new List<string>() { "a", "b" },
            };
        }

    }
}
