// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Microsoft.Web.LibraryManager.Test
{
    [TestClass]
    public class LibraryInstallationStateTest
    {
        [TestMethod]
        public void FromInterface()
        {
            var state = new Mocks.LibraryInstallationState
            {
                ProviderId = "_prov_",
                LibraryId = "_lib_",
                DestinationPath = "_path_",
                Files = new List<string>() { "a", "b" },
            };

            var lis = LibraryInstallationState.FromInterface(state);
            Assert.AreEqual(state.ProviderId, lis.ProviderId);
            Assert.AreEqual(state.LibraryId, lis.LibraryId);
            Assert.AreEqual(state.DestinationPath, lis.DestinationPath);
            Assert.AreEqual(state.Files, lis.Files);
        }
    }
}
