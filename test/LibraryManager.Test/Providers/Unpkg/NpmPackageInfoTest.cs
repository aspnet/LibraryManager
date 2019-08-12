using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Providers.Unpkg;
using Newtonsoft.Json.Linq;

namespace Microsoft.Web.LibraryManager.Test.Providers.Unpkg
{
    [TestClass]
    public class NpmPackageInfoTest
    {
        [TestMethod]
        public void NpmPackageInfo_Parse_ValidJsonInfo()
        {
            string json = @"{
  ""name"": ""jquery"",
  ""title"": ""jQuery"",
  ""description"": ""JavaScript library for DOM operations"",
  ""version"": ""3.3.1"",
  ""main"": ""dist/jquery.js"",
  ""homepage"": ""https://jquery.com"",
  ""author"": {
    ""name"": ""JS Foundation and other contributors"",
    ""url"": ""https://github.com/jquery/jquery/blob/3.3.1/AUTHORS.txt""
  },
  ""repository"": {
    ""type"": ""git"",
    ""url"": ""git+https://github.com/jquery/jquery.git""
  },
  ""bugs"": {
    ""url"": ""https://github.com/jquery/jquery/issues""
  },
  ""license"": ""MIT"",
  ""dependencies"": {}
}";

            JObject packageInfoJson = JObject.Parse(json);
            NpmPackageInfo packageInfo = NpmPackageInfo.Parse(packageInfoJson);

            Assert.AreEqual("jquery", packageInfo.Name);
            Assert.AreEqual("JavaScript library for DOM operations", packageInfo.Description);
            Assert.AreEqual("3.3.1", packageInfo.LatestVersion);
        }

        [TestMethod]
        public void NpmPackageInfo_Parse_InvalidJsonInfo()
        {
            string json = @"{
  ""invlidForm"": """"
}";

            JObject packageInfoJson = JObject.Parse(json);
            NpmPackageInfo packageInfo = NpmPackageInfo.Parse(packageInfoJson);

            Assert.AreEqual(string.Empty, packageInfo.Name);
            Assert.AreEqual(string.Empty, packageInfo.Description);
            Assert.AreEqual(string.Empty, packageInfo.LatestVersion);
        }
    }
}
