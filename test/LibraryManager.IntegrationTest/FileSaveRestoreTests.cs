using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Web.LibraryManager.IntegrationTest
{
    [TestClass]
    public class FileSaveRestoreTests : VisualStudioLibmanHostTest
    {
        [TestMethod]
        public void FileSaveRestore_AddDeleteLibrary()
        {
            string projectPath = Path.Combine(SolutionRootPath, _projectName);

            _libmanConfig.Delete();
            Helpers.FileIO.WaitForDeletedFile(projectPath, Path.Combine(projectPath, _libman), caseInsensitive: false, timeout: 1000);

            VisualStudio.ObjectModel.Commanding.ExecuteCommand("Project.ManageClientSideLibraries");
            Helpers.FileIO.WaitForRestoredFile(projectPath, Path.Combine(projectPath, _libman), caseInsensitive: false, timeout: 1000);

            _libmanConfig = _webProject[_libman];
            _libmanConfig.Open();

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
        }

        private void ReplaceFileContent(string content)
        {
            Editor.Selection.SelectAll();
            Editor.KeyboardCommands.Delete();
            Editor.Edit.InsertTextInBuffer(content);

            _libmanConfig.Save();
        }
    }
}
