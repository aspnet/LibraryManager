using System;
using System.IO;
using System.Threading;
using Microsoft.Test.Apex.VisualStudio;
using Microsoft.Test.Apex.VisualStudio.Solution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Web.LibraryManager.IntegrationTest
{
    [TestClass]
    public class FileSaveRestoreTests : VisualStudioLibmanHostTest
    {
        const string _projectName = @"TestProjectCore20";
        const string _libman = "libman.json";

        [TestMethod]
        public void FileSaveRestore_AddDeleteLibrary()
        {
            ProjectTestExtension webProject = Solution[_projectName];
            ProjectItemTestExtension libManConfig = webProject[_libman];
            string projectPath = Path.Combine(SolutionRootPath, _projectName);

            libManConfig.Delete();
            LibmanTestsUtility.WaitForDeletedFile(projectPath, Path.Combine(projectPath, _libman), caseInsensitive: false, timeout: 1000);

            VisualStudio.ObjectModel.Commanding.ExecuteCommand("Project.ManageClientSideLibraries");
            LibmanTestsUtility.WaitForRestoredFile(projectPath, Path.Combine(projectPath, _libman), caseInsensitive: false, timeout: 1000);

            libManConfig = webProject[_libman];
            libManConfig.Open();

            string pathToLibrary = Path.Combine(SolutionRootPath, _projectName, "wwwroot", "lib", "jquery-validate");
            string[] expectedFiles = new[]
            {
                Path.Combine(pathToLibrary, "jquery.validate.js"),
                Path.Combine(pathToLibrary, "localization", "messages_ar.js"),
            };

            Editor.Caret.MoveToExpression("\"libraries\"");
            Editor.Caret.MoveToEndOfLine();
            Editor.Caret.MoveLeft();
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("{");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"destination\":");
            LibmanTestsUtility.WaitForCompletionEntries(Editor, new string[] { }, caseInsensitive: true);
            Editor.KeyboardCommands.Type("wwwroot/lib/jquery-validate");
            Editor.Caret.MoveRight();
            Editor.KeyboardCommands.Type(",");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"library\": \"jquery-validate@1.17.0\"");

            libManConfig.Save();
            LibmanTestsUtility.WaitForRestoredFiles(pathToLibrary, expectedFiles, caseInsensitive: true);

            Editor.Caret.MoveToExpression("{", 0, 2);
            for (int i = 0; i < 4; ++i)
            {
                Editor.Edit.DeleteLine();
            }

            libManConfig.Save();
            LibmanTestsUtility.WaitForDeletedFiles(pathToLibrary, expectedFiles, caseInsensitive: true);
        }
    }
}
