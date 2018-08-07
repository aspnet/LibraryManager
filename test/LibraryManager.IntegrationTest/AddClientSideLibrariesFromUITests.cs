using System.IO;
using Microsoft.Test.Apex.VisualStudio.Shell.ToolWindows;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.IntegrationTest.Services;

namespace Microsoft.Web.LibraryManager.IntegrationTest
{
    [TestClass]
    public class AddClientSideLibrariesFromUITests : VisualStudioLibmanHostTest
    {
        protected override void DoHostTestInitialize()
        {
            base.DoHostTestInitialize();

            string libmanConfigFullPath = _libmanConfig.FullPath;

            if (File.Exists(libmanConfigFullPath))
            {
                string projectPath = Path.Combine(SolutionRootPath, _projectName);
                _libmanConfig.Delete();
                Helpers.FileIO.WaitForDeletedFile(projectPath, libmanConfigFullPath, caseInsensitive: false);
            }
        }

        [TestMethod]
        public void InstallClientSideLibraries_FromProjectRoot_SmokeTest()
        {
            SetLibraryAndClickInstall(_projectName, "jquery-validate@1.17.0");

            string pathToLibrary = Path.Combine(SolutionRootPath, _projectName, "wwwroot", "lib", "jquery-validate");
            string[] expectedFiles = new[]
            {
                Path.Combine(pathToLibrary, "jquery.validate.js"),
                Path.Combine(pathToLibrary, "localization", "messages_ar.js"),
            };

            string manifestContents = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""libraries"": [
    {
      ""library"": ""jquery-validate@1.17.0"",
      ""destination"": ""wwwroot/lib/jquery-validate/""
    }
  ]
}";
            Helpers.FileIO.WaitForRestoredFiles(pathToLibrary, expectedFiles, caseInsensitive: true, timeout: 20000);
            Assert.AreEqual(manifestContents, File.ReadAllText(_pathToLibmanFile));
        }

        [TestMethod]
        public void InstallClientSideLibraries_FromFolder_SmokeTest()
        {
            SetLibraryAndClickInstall("wwwroot", "jquery-validate@1.17.0");

            string pathToLibrary = Path.Combine(SolutionRootPath, _projectName, "wwwroot", "jquery-validate");
            string[] expectedFiles = new[]
            {
                Path.Combine(pathToLibrary, "jquery.validate.js"),
                Path.Combine(pathToLibrary, "localization", "messages_ar.js"),
            };

            string manifestContents = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""libraries"": [
    {
      ""library"": ""jquery-validate@1.17.0"",
      ""destination"": ""wwwroot/jquery-validate/""
    }
  ]
}";
            Helpers.FileIO.WaitForRestoredFiles(pathToLibrary, expectedFiles, caseInsensitive: true, timeout: 20000);
            Assert.AreEqual(manifestContents, File.ReadAllText(_pathToLibmanFile));
        }

        private void SetLibraryAndClickInstall(string nodeName, string library)
        {
            SolutionExplorerItemTestExtension solutionExplorerItemTestExtension = SolutionExplorer.FindItemRecursive(nodeName);
            solutionExplorerItemTestExtension.Select();

            InstallDialogTestService installDialogTestService = VisualStudio.Get<InstallDialogTestService>();
            InstallDialogTestExtension installDialogTestExtenstion = installDialogTestService.OpenDialog();

            installDialogTestExtenstion.SetLibrary(library);
            installDialogTestExtenstion.ClickInstall();
        }
    }
}
