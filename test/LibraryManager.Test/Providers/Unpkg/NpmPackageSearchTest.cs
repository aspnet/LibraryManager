using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Providers.Unpkg;
using Moq;

namespace Microsoft.Web.LibraryManager.Test.Providers.Unpkg
{
    [TestClass]
    public class NpmPackageSearchTest
    {
        [TestMethod]
        public async Task NpmPackageSearch_GetPackageNamesAsync_NullSearchItem_DoesNotMakeWebRequest()
        {
            var mockRequestHandler = new Mock<IWebRequestHandler>();
            var sut = new NpmPackageSearch(mockRequestHandler.Object);

            IEnumerable<NpmPackageInfo> packages = await sut.GetPackageNamesAsync(null, CancellationToken.None);

            mockRequestHandler.Verify(x => x.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task NpmPackageSearch_GetPackageNamesAsync_ResponseContainsNoObjects_ReturnEmptyListOfPackages()
        {
            string noHitsResponse = @"{""objects"":[],""total"":0}";
            var mockRequestHandler = new Mock<IWebRequestHandler>();
            mockRequestHandler.Setup(m => m.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                              .Returns(Task.FromResult<Stream>(new MemoryStream(Encoding.Default.GetBytes(noHitsResponse))));
            var sut = new NpmPackageSearch(mockRequestHandler.Object);

            IEnumerable<NpmPackageInfo> packages = await sut.GetPackageNamesAsync("searchTerm", CancellationToken.None);

            Assert.IsFalse(packages.Any());
        }

        [TestMethod]
        public async Task NpmPackageSearch_GetPackageNamesAsync_UnScopedPackage()
        {
            // this is a mockup of the response from the NPM registry search
            string response = @"{
    ""objects"": [
        {
                ""package"": {
                ""name"": ""firstResult"",
                ""scope"": ""unscoped"",
                ""version"": ""1.0.1"",
                ""description"": ""a package"",
            }
            },
        {
                ""package"": {
                ""name"": ""secondResult"",
                ""scope"": ""unscoped"",
                ""version"": ""2.2.0"",
                ""description"": ""another package""
                }
            }
    ],
    ""total"": 2
}";

            var mockRequestHandler = new Mock<IWebRequestHandler>();
            mockRequestHandler.Setup(m => m.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                              .Returns(Task.FromResult<Stream>(new MemoryStream(Encoding.Default.GetBytes(response))));
            var sut = new NpmPackageSearch(mockRequestHandler.Object);

            IEnumerable<NpmPackageInfo> result = await sut.GetPackageNamesAsync("searchTerm", CancellationToken.None);

            CollectionAssert.AreEquivalent(new[] { "firstResult", "secondResult" },
                                           result.Select(p => p.Name).ToList());
        }

        [TestMethod]
        public async Task NpmPackageSearch_GetPackageNamesAsync_ScopedPackage()
        {
            // this is a mockup of the response from the npms.io search
            string response = @"{
    ""results"": [
        {
            ""package"": {
                ""name"": ""firstResult"",
                ""scope"": ""unscoped"",
                ""version"": ""1.0.1"",
                ""description"": ""a package"",
            }
            },
        {
            ""package"": {
                ""name"": ""secondResult"",
                ""scope"": ""unscoped"",
                ""version"": ""2.2.0"",
                ""description"": ""another package""
                }
            }
    ],
    ""total"": 2
}";

            var mockRequestHandler = new Mock<IWebRequestHandler>();
            mockRequestHandler.Setup(m => m.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                              .Returns(Task.FromResult<Stream>(new MemoryStream(Encoding.Default.GetBytes(response))));
            var sut = new NpmPackageSearch(mockRequestHandler.Object);

            IEnumerable<NpmPackageInfo> result = await sut.GetPackageNamesAsync("@searchTerm/", CancellationToken.None);

            CollectionAssert.AreEquivalent(new[] { "firstResult", "secondResult" },
                                           result.Select(p => p.Name).ToList());
        }
    }
}
