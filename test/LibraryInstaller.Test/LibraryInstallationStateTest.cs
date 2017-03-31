using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace LibraryInstaller.Test
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
                Path = "_path_",
                Files = new List<string>() { "a", "b" },
            };

            var lis = LibraryInstallationState.FromInterface(state);
            Assert.AreEqual(state.ProviderId, lis.ProviderId);
            Assert.AreEqual(state.LibraryId, lis.LibraryId);
            Assert.AreEqual(state.Path, lis.Path);
            Assert.AreEqual(state.Files, lis.Files);
        }
    }
}
