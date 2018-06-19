using System.IO;
using Microsoft.Test.Apex.VisualStudio.Solution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Web.LibraryManager.IntegrationTest
{
    [TestClass]
    public class FileSaveRestoreTests : VisualStudioLibmanHostTest
    {
        ProjectTestExtension _webProject;
        ProjectItemTestExtension _libManConfig;
        const string _projectName = @"TestProjectCore20";
        const string _libman = "libman.json";
        private string _libmanFileContent;

        [TestInitialize]
        public void initialize()
        {
            _webProject = Solution[_projectName];
            _libManConfig = _webProject[_libman];
            string pathToLibmanFile = Path.Combine(SolutionRootPath, _projectName, _libman);
            _libmanFileContent = File.ReadAllText(pathToLibmanFile);
        }

        [TestMethod]
        public void FileSaveRestore_AddDeleteLibrary()
        {
            string projectPath = Path.Combine(SolutionRootPath, _projectName);

            _libManConfig.Delete();
            Helpers.FileIO.WaitForDeletedFile(projectPath, Path.Combine(projectPath, _libman), caseInsensitive: false, timeout: 1000);

            VisualStudio.ObjectModel.Commanding.ExecuteCommand("Project.ManageClientSideLibraries");
            Helpers.FileIO.WaitForRestoredFile(projectPath, Path.Combine(projectPath, _libman), caseInsensitive: false, timeout: 1000);

            _libManConfig = _webProject[_libman];
            _libManConfig.Open();

            string pathToLibrary = Path.Combine(SolutionRootPath, _projectName, "wwwroot", "lib", "jquery-validate");
            string[] expectedFiles = new[]
            {
                Path.Combine(pathToLibrary, "jquery.validate.js"),
                Path.Combine(pathToLibrary, "localization", "messages_ar.js"),
            };
            string addingLibraryContent = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""libraries"": [
    {
      ""library"": ""jquery-validate@1.17.0"",
      ""destination"": ""wwwroot/lib/jquery-validate""
    }
  ]
}";
            string deletingLibraryContent = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""libraries"": []
}";

            ReplaceFileContent(addingLibraryContent);
            Helpers.FileIO.WaitForRestoredFiles(pathToLibrary, expectedFiles, caseInsensitive: true);

            ReplaceFileContent(deletingLibraryContent);
            Helpers.FileIO.WaitForDeletedFiles(pathToLibrary, expectedFiles, caseInsensitive: true);

            ReplaceFileContent(_libmanFileContent);
        }

        private void ReplaceFileContent(string content)
        {
            Editor.Selection.SelectAll();
            Editor.KeyboardCommands.Backspace();
            Editor.Edit.InsertTextInBuffer(content);

            _libManConfig.Save();
        }
    }
}
