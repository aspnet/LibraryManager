using System;
using System.IO;
using Microsoft.Test.Apex.VisualStudio;
using Microsoft.Test.Apex.VisualStudio.Solution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Web.LibraryManager.IntegrationTest
{
    [TestClass]
    public class FileSaveRestoreTests : VisualStudioLibmanHostTest
    {
        ProjectTestExtension _webProject;
        ProjectItemTestExtension _libManConfig;

        [TestInitialize()]
        public void initialize()
        {
            string projectName = "TestProjectCore20";

            _webProject = Solution.ProjectsRecursive[projectName];
            _libManConfig = _webProject.Find(SolutionItemFind.FileName, "libman.json");
        }

        [TestMethod]
        public void FileSaveRestore_Cdnjs_AddNewLibrariesWithoutSpecifingFiles()
        {
            _libManConfig.Open();
            string[] expectedFilesAndFolders = new[] {
                "jquery.js",
            };

            Editor.Caret.MoveDown();
            Editor.Caret.MoveToEndOfLine();
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"defaultProvider\": \"cdnjs\",");

            Editor.Caret.MoveDown(3);
            Editor.KeyboardCommands.Type("\"library\": \"jquery@3.3.1\",");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"destination\":");
            Editor.KeyboardCommands.Type("wwwroot/lib/jquery");

            _libManConfig.Save();
            string cwd = Path.Combine(Path.GetDirectoryName(_webProject.FullPath), "wwwroot", "lib", "jquery");
            LibmanTestsUtility.WaitForRestoredFiles(cwd, expectedFilesAndFolders, caseInsensitive: true);
        }
    }
}
