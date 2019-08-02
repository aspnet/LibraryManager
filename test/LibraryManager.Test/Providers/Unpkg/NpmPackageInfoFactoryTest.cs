using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Providers.Unpkg;

namespace Microsoft.Web.LibraryManager.Test.Providers.Unpkg
{
    [TestClass]
    public class NpmPackageInfoFactoryTest
    {
        [TestMethod]
        public async Task NpmPackageSearch_GetPackageInfoAsync_UnScopedPackage()
        {
            string searchItem = "jquery";
            CancellationToken token = CancellationToken.None;

            var sut = new NpmPackageInfoFactory();
            NpmPackageInfo packageInfo = await sut.GetPackageInfoAsync(searchItem, token);

            Assert.IsTrue(packageInfo.Versions != null);
            Assert.IsTrue(packageInfo.Versions.Count() > 0);
        }

        [TestMethod]
        public async Task NpmPackageSearch_GetPackageInfoAsync_ScopedPackage()
        {
            string searchItem = "@angular/cli";
            CancellationToken token = CancellationToken.None;

            var sut = new NpmPackageInfoFactory();
            NpmPackageInfo packageInfo = await sut.GetPackageInfoAsync(searchItem, token);

            Assert.IsTrue(packageInfo.Versions != null);
            Assert.IsTrue(packageInfo.Versions.Count() > 0);
        }
    }
}
