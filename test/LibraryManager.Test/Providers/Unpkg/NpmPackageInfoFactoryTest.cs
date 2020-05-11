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
            var expectedVersions = (new[] { "1.0.1", "2.1.7", "3.1.4-pi" })
                                            .Select(x => SemanticVersion.Parse(x))
                                            .ToList();
            string packageLatestRequest = "https://registry.npmjs.org/jquery/latest";
            string packageInfoRequest = "https://registry.npmjs.org/jquery";
            var requestHandler = new Mocks.WebRequestHandler();
            requestHandler.ArrangeResponse(packageLatestRequest, FakeResponses.FakeLibraryLatest)
                          .ArrangeResponse(packageInfoRequest, FakeResponses.FakeLibraryWithVersions);
            var sut = new NpmPackageInfoFactory(requestHandler);

            NpmPackageInfo packageInfo = await sut.GetPackageInfoAsync(searchItem, CancellationToken.None);

            Assert.AreEqual("fakelibrary", packageInfo.Name);
            Assert.AreEqual("fake description", packageInfo.Description);
            CollectionAssert.AreEquivalent(expectedVersions, packageInfo.Versions.ToList());
        }

        [TestMethod]
        public async Task NpmPackageSearch_GetPackageInfoAsync_ScopedPackage()
        {
            string searchItem = "@angular/cli";
            var expectedVersions = (new[] { "1.0.1", "2.1.7", "3.1.4-pi" })
                                            .Select(x => SemanticVersion.Parse(x))
                                            .ToList();
            string packageInfoRequest = "https://registry.npmjs.org/@angular%2fcli";
            var requestHandler = new Mocks.WebRequestHandler();
            requestHandler.ArrangeResponse(packageInfoRequest, FakeResponses.FakeLibraryWithVersions);
            var sut = new NpmPackageInfoFactory(requestHandler);

            NpmPackageInfo packageInfo = await sut.GetPackageInfoAsync(searchItem, CancellationToken.None);

            Assert.AreEqual("fakelibrary", packageInfo.Name);
            Assert.AreEqual("fake description", packageInfo.Description);
            CollectionAssert.AreEquivalent(expectedVersions, packageInfo.Versions.ToList());
        }

        private class FakeResponses
        {
            public const string FakeLibraryLatest = @"{
    ""name"": ""fakelibrary"",
    ""description"": ""fake description"",
    ""versions"": ""2.1.7""
}";
            public const string FakeLibraryWithVersions = @"{
    ""name"": ""fakelibrary"",
    ""description"": ""fake description"",
    ""versions"": {
        ""1.0.1"": null,
        ""2.1.7"": null,
        ""3.1.4-pi"": null
    }
}";
        }
    }
}
