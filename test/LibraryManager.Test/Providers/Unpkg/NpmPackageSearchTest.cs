using System;
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

            IEnumerable<Tuple<string, string>> packagesAndCurrentVersions = await NpmPackageSearch.GetPackageNamesAndCurrentVersionsAsync(searchItem, token);

            foreach (Tuple<string, string> nameAndVersion in packagesAndCurrentVersions)
            {
                Assert.IsTrue(!string.IsNullOrWhiteSpace(nameAndVersion.Item1));
                Assert.IsTrue(!string.IsNullOrWhiteSpace(nameAndVersion.Item2));
            }

            Assert.AreEqual(expectedCount, packagesAndCurrentVersions.Count());
        }

        [TestMethod]
        public async Task NpmPackageSearch_GetPackageNamesAsync_UnScopedPackage()
        {
            string searchItem = "jquery";
            CancellationToken token = CancellationToken.None;

            IEnumerable<Tuple<string, string>> packagesAndCurrentVersions = await NpmPackageSearch.GetPackageNamesAndCurrentVersionsAsync(searchItem, token);

            foreach(Tuple<string, string> nameAndVersion in packagesAndCurrentVersions)
            {
                Assert.IsTrue(!string.IsNullOrWhiteSpace(nameAndVersion.Item1));
                Assert.IsTrue(!string.IsNullOrWhiteSpace(nameAndVersion.Item2));
            }

            Assert.AreEqual(100, packagesAndCurrentVersions.Count());
            Assert.AreEqual("jquery", packagesAndCurrentVersions.FirstOrDefault().Item1);
        }

        [TestMethod]
        public async Task NpmPackageSearch_GetPackageNamesAsync_ScopedPackage()
        {
            string searchItem = "@angular/";
            CancellationToken token = CancellationToken.None;

            IEnumerable<Tuple<string, string>> packagesAndCurrentVersions = await NpmPackageSearch.GetPackageNamesAndCurrentVersionsAsync(searchItem, token);

            foreach (Tuple<string, string> nameAndVersion in packagesAndCurrentVersions)
            {
                Assert.IsTrue(!string.IsNullOrWhiteSpace(nameAndVersion.Item1));
                Assert.IsTrue(!string.IsNullOrWhiteSpace(nameAndVersion.Item2));
            }

            Assert.IsTrue(packagesAndCurrentVersions.Count() > 0);
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
