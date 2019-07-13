using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Providers.Unpkg;

namespace Microsoft.Web.LibraryManager.Test.Providers.Unpkg
{
    [TestClass]
    public class NpmPackageSearchTest
    {
        [DataTestMethod]
        [DataRow(null, 0)]
        [DataRow("", 0)]
        [DataRow("poiuytrewq", 0)]
        public async Task NpmPackageSearch_GetPackageNamesAsync_NullOrEmptyOrUnmatchedPackage(string searchItem, int expectedCount)
        {
            CancellationToken token = CancellationToken.None;

            IEnumerable<string> packages = await NpmPackageSearch.GetPackageNamesAsync(searchItem, token);

            Assert.AreEqual(expectedCount, packages.Count());
        }

        [TestMethod]
        public async Task NpmPackageSearch_GetPackageNamesAsync_UnScopedPackage()
        {
            string searchItem = "jquery";
            CancellationToken token = CancellationToken.None;

            IEnumerable<string> packages = await NpmPackageSearch.GetPackageNamesAsync(searchItem, token);

            Assert.AreEqual(100, packages.Count());
            Assert.AreEqual("jquery", packages.FirstOrDefault());
        }

        [TestMethod]
        public async Task NpmPackageSearch_GetPackageNamesAsync_ScopedPackage()
        {
            string searchItem = "@angular/";
            CancellationToken token = CancellationToken.None;

            IEnumerable<string> packages = await NpmPackageSearch.GetPackageNamesAsync(searchItem, token);

            Assert.IsTrue(packages.Count() > 0);
        }

        [TestMethod]
        public async Task NpmPackageSearch_GetPackageInfoAsync_UnScopedPackage()
        {
            string searchItem = "jquery";
            CancellationToken token = CancellationToken.None;

            NpmPackageInfo packageInfo = await NpmPackageSearch.GetPackageInfoAsync(searchItem, token);

            Assert.IsTrue(packageInfo.Versions != null);
            Assert.IsTrue(packageInfo.Versions.Count() > 0);
        }

        [TestMethod]
        public async Task NpmPackageSearch_GetPackageInfoAsync_ScopedPackage()
        {
            string searchItem = "@angular/cli";
            CancellationToken token = CancellationToken.None;

            NpmPackageInfo packageInfo = await NpmPackageSearch.GetPackageInfoAsync(searchItem, token);

            Assert.IsTrue(packageInfo.Versions != null);
            Assert.IsTrue(packageInfo.Versions.Count() > 0);
        }
    }
}
