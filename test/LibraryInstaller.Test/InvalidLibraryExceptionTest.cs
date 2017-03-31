using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibraryInstaller.Test
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
