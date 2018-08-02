using System;
using System.IO;
using Microsoft.Test.Apex.Services;
using Microsoft.Test.Apex.VisualStudio.Shell;
using Microsoft.Test.Apex.VisualStudio.Shell.ToolWindows;
using Microsoft.Test.Apex.VisualStudio.Solution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.IntegrationTest.Services;

namespace Microsoft.Web.LibraryManager.IntegrationTest
{
    [TestClass]
    public class AddClientSideLibrariesFromUITests : VisualStudioLibmanHostTest
    {
        private string _initialLibmanFileContent;
        private string _pathToLibmanFile;
        private ProjectTestExtension _webProject;
        private const string _libman = "libman.json";
        private const string _projectName = @"TestProjectCore20";

        protected override void DoHostTestInitialize()
        {
            base.DoHostTestInitialize();

            _webProject = Solution[_projectName];
            ProjectItemTestExtension libmanConfig = _webProject[_libman];
            _pathToLibmanFile = Path.Combine(SolutionRootPath, _projectName, _libman);
            _initialLibmanFileContent = File.ReadAllText(_pathToLibmanFile);

            string libmanConfigFullPath = libmanConfig.FullPath;

            if (File.Exists(libmanConfigFullPath))
            {
                string projectPath = Path.Combine(SolutionRootPath, _projectName);
                libmanConfig.Delete();
                Helpers.FileIO.WaitForDeletedFile(projectPath, libmanConfigFullPath, caseInsensitive: false);
            }
        }

        protected override void DoHostTestCleanup()
        {
            ProjectItemTestExtension libmanConfig = _webProject[_libman];

            if (libmanConfig != null)
            {
                CleanClientSideLibraries();

                libmanConfig.Open();
                Editor.Selection.SelectAll();
                Editor.KeyboardCommands.Delete();
                Editor.Edit.InsertTextInBuffer(_initialLibmanFileContent);

                libmanConfig.Save();
            }

            base.DoHostTestCleanup();
        }

        private void CleanClientSideLibraries()
        {
            Guid guid = Guid.Parse("44ee7bda-abda-486e-a5fe-4dd3f4cefac1");
            uint commandId = 0x0200;
            SolutionExplorerItemTestExtension configFileNode = SolutionExplorer.FindItemRecursive(_libman);
            configFileNode.Select();

            WaitFor.IsTrue(() =>
            {
                CommandQueryResult queryResult = VisualStudio.ObjectModel.Commanding.QueryStatusCommand(guid, commandId);
                return queryResult.IsEnabled;
            },TimeSpan.FromMilliseconds(40000), TimeSpan.FromMilliseconds(500));

            VisualStudio.ObjectModel.Commanding.ExecuteCommand(guid, commandId, null);
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
