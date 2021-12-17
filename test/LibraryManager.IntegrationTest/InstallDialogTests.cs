using System.IO;
using Microsoft.Test.Apex.VisualStudio.Shell;
using Microsoft.Test.Apex.VisualStudio.Shell.ToolWindows;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.IntegrationTest.Services;

namespace Microsoft.Web.LibraryManager.IntegrationTest
{
    [TestClass]
    public class InstallDialogTests : VisualStudioLibmanHostTest
    {
        [TestMethod]
        public void InstallClientSideLibraries_FromProjectRoot_SmokeTest()
        {
            RemoveExistingManifest();

            InstallDialogTestExtension installDialogTestExtension = OpenWizardFromSolutionExplorerItem(ProjectName);
            installDialogTestExtension.Library = "jquery-validate@1.17.0";
            installDialogTestExtension.WaitForFileSelectionsAvailable();
            installDialogTestExtension.ClickInstall();

            string pathToLibrary = Path.Combine(SolutionRootPath, ProjectName, "wwwroot", "lib", "jquery-validate");
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
            RemoveExistingManifest();

            InstallDialogTestExtension installDialogTestExtension = OpenWizardFromSolutionExplorerItem("wwwroot");
            installDialogTestExtension.Library = "jquery-validate@1.17.0";
            installDialogTestExtension.WaitForFileSelectionsAvailable();
            installDialogTestExtension.ClickInstall();

            string pathToLibrary = Path.Combine(SolutionRootPath, ProjectName, "wwwroot", "jquery-validate");
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

        /*
         * This test verifies that we load valid metadata (namely, list of files) when using the form library@latest.
         * Even though "latest" isn't a real version (hence it normally wouldn't show any files), the wizard should
         * still fetch the correct data and show a list of files.
         */
        [DataTestMethod]
        [DataRow("unpkg")]
        [DataRow("jsdelivr")]
        public void NpmPackageWithLatestTag_LoadsFileMetadata(string provider)
        {
            InstallDialogTestExtension installDialogTestExtenstion = OpenWizardFromSolutionExplorerItem("wwwroot");

            try
            {
                installDialogTestExtenstion.Provider = provider;
                installDialogTestExtenstion.Library = "jquery@latest";

                installDialogTestExtenstion.WaitForFileSelectionsAvailable();
            }
            finally
            {
                installDialogTestExtenstion.Close();
            }
        }

        [TestMethod]
        public void InstallClientSideLibraries_DefaultProviderIsSelectedOnDialogOpen()
        {
            _libmanConfig = _webProject[LibManManifestFile];
            DocumentWindowTestExtension document = _libmanConfig.Open();

            Editor.Caret.MoveToExpression("version");
            Editor.Caret.MoveToEndOfLine();
            Editor.KeyboardCommands.Enter();
            Editor.Edit.InsertTextInBuffer(@"""defaultProvider"": ""jsdelivr"",");

            InstallDialogTestExtension installDialogTestExtension = OpenWizardFromSolutionExplorerItem("wwwroot");

            Verify.Strings.AreEqual("jsdelivr", installDialogTestExtension.Provider);
            installDialogTestExtension.Close();

            document.Close(saveIfDirty: false);
        }

        private void RemoveExistingManifest()
        {
            string libmanConfigFullPath = _libmanConfig.FullPath;

            if (File.Exists(libmanConfigFullPath))
            {
                string projectPath = Path.Combine(SolutionRootPath, ProjectName);
                _libmanConfig.Delete();
                Helpers.FileIO.WaitForDeletedFile(projectPath, libmanConfigFullPath, caseInsensitive: false);
            }
        }

        private InstallDialogTestExtension OpenWizardFromSolutionExplorerItem(string nodeName)
        {
            SolutionExplorerItemTestExtension solutionExplorerItemTestExtension = SolutionExplorer.FindItemRecursive(nodeName);
            solutionExplorerItemTestExtension.Select();

            InstallDialogTestService installDialogTestService = VisualStudio.Get<InstallDialogTestService>();
            InstallDialogTestExtension installDialogTestExtenstion = installDialogTestService.OpenDialog();
            return installDialogTestExtenstion;
        }
    }
}
