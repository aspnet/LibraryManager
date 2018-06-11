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
        ProjectTestExtension _webProject;
        ProjectItemTestExtension _libManConfig;
        const string _projectName = @"TestProjectCore20";

        [TestInitialize()]
        public void initialize()
        {
            _webProject = Solution.ProjectsRecursive[_projectName];
            _libManConfig = _webProject.Find(SolutionItemFind.FileName, "libman.json");
        }

        [TestCleanup]
        public void Cleanup()
        {
            int count = Editor.Edit.UndoStack.Count;
            Editor.Edit.Undo(count);

            _libManConfig.Save();

            // Delete restored libraries
            string deletePath = Path.Combine(SolutionRootPath, _projectName, "wwwroot", "lib");
            Directory.Delete(deletePath, true);
        }

        [TestMethod]
        public void FileSaveRestore_Cdnjs_AddNewLibraryWithoutSpecifingFiles()
        {
            _libManConfig.Open();
            string[] expectedFilesAndFolders = new[] {
                "jquery.js",
                "jquery.min.js",
            };

            Editor.Caret.MoveToExpression("\"version\": \"1.0\"");
            Editor.Caret.MoveToEndOfLine();
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"defaultProvider\":");
            LibmanTestsUtility.WaitForCompletionEntry(Editor, "cdnjs", caseInsensitive: true);
            Editor.KeyboardCommands.Type("cdnjs");

            Editor.Caret.MoveToExpression("\"libraries\"");
            Editor.Caret.MoveDown(2);
            Editor.KeyboardCommands.Type("\"library\":");
            LibmanTestsUtility.WaitForCompletionEntry(Editor, "jquery", caseInsensitive: true, timeout: 5000);
            Editor.KeyboardCommands.Type("jquery@3.3.1");
            Editor.KeyboardCommands.Right();
            Editor.KeyboardCommands.Type(",");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"destination\":");
            Editor.KeyboardCommands.Type("wwwroot/lib/jquery");
            
            _libManConfig.Save();
            string cwd = Path.Combine(Path.GetDirectoryName(_webProject.FullPath), "wwwroot", "lib", "jquery");
            LibmanTestsUtility.WaitForRestoredFiles(cwd, expectedFilesAndFolders, caseInsensitive: true);
        }

        [TestMethod]
        public void FileSaveRestore_Unpkg_AddNewLibraryWithoutSpecifingFiles()
        {
            _libManConfig.Open();
            string[] expectedFilesAndFolders = new[] {
                "LICENSE.txt",
                "dist",
            };

            Editor.Caret.MoveToExpression("\"version\": \"1.0\"");
            Editor.Caret.MoveToEndOfLine();
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"defaultProvider\":");
            LibmanTestsUtility.WaitForCompletionEntry(Editor, "unpkg", caseInsensitive: true);
            Editor.KeyboardCommands.Type("unpkg");

            Editor.Caret.MoveToExpression("\"libraries\"");
            Editor.Caret.MoveDown(2);
            Editor.KeyboardCommands.Type("\"library\":");
            Editor.KeyboardCommands.Type("jquery@3.3.1");
            Editor.KeyboardCommands.Right();
            Editor.KeyboardCommands.Type(",");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"destination\":");
            Editor.KeyboardCommands.Type("wwwroot/lib/jquery");

            _libManConfig.Save();
            string cwd = Path.Combine(Path.GetDirectoryName(_webProject.FullPath), "wwwroot", "lib", "jquery");
            LibmanTestsUtility.WaitForRestoredFiles(cwd, expectedFilesAndFolders, caseInsensitive: true);
        }

        [TestMethod]
        public void FileSaveRestore_Cdnjs_AddNewLibraryWithSpecifingFiles()
        {
            _libManConfig.Open();

            Editor.Caret.MoveToExpression("\"version\": \"1.0\"");
            Editor.Caret.MoveToEndOfLine();
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"defaultProvider\":");
            LibmanTestsUtility.WaitForCompletionEntry(Editor, "cdnjs", caseInsensitive: true);
            Editor.KeyboardCommands.Type("cdnjs");

            Editor.Caret.MoveToExpression("\"libraries\"");
            Editor.Caret.MoveDown(2);
            Editor.KeyboardCommands.Type("\"library\":");
            LibmanTestsUtility.WaitForCompletionEntry(Editor, "jquery", caseInsensitive: true, timeout: 5000);
            Editor.KeyboardCommands.Type("jquery@3.3.1");
            Editor.KeyboardCommands.Right();
            Editor.KeyboardCommands.Type(",");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"destination\":");
            Editor.KeyboardCommands.Type("wwwroot/lib/jquery");

            Editor.KeyboardCommands.Right();
            Editor.KeyboardCommands.Type(",");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"files\": [");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"jquery.js\"");

            _libManConfig.Save();
            string cwd = Path.Combine(Path.GetDirectoryName(_webProject.FullPath), "wwwroot", "lib", "jquery");
            LibmanTestsUtility.WaitForRestoredFile(cwd, "jquery.js", caseInsensitive: true);
            LibmanTestsUtility.WaitForRestoredFileNotPresent(cwd, "jquery.min.js", caseInsensitive: true);
        }

        [TestMethod]
        public void FileSaveRestore_Unpkg_AddNewLibraryWithSpecifingFiles()
        {
            _libManConfig.Open();

            Editor.Caret.MoveToExpression("\"version\": \"1.0\"");
            Editor.Caret.MoveToEndOfLine();
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"defaultProvider\":");
            LibmanTestsUtility.WaitForCompletionEntry(Editor, "unpkg", caseInsensitive: true);
            Editor.KeyboardCommands.Type("unpkg");

            Editor.Caret.MoveToExpression("\"libraries\"");
            Editor.Caret.MoveDown(2);
            Editor.KeyboardCommands.Type("\"library\":");
            Editor.KeyboardCommands.Type("jquery@3.3.1");
            Editor.KeyboardCommands.Right();
            Editor.KeyboardCommands.Type(",");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"destination\":");
            Editor.KeyboardCommands.Type("wwwroot/lib/jquery");

            Editor.KeyboardCommands.Right();
            Editor.KeyboardCommands.Type(",");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"files\": [");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"LICENSE.txt\"");

            _libManConfig.Save();
            string cwd = Path.Combine(Path.GetDirectoryName(_webProject.FullPath), "wwwroot", "lib", "jquery");
            LibmanTestsUtility.WaitForRestoredFile(cwd, "LICENSE.txt", caseInsensitive: true);
            LibmanTestsUtility.WaitForRestoredFileNotPresent(cwd, "dist", caseInsensitive: true);
        }

        [TestMethod]
        public void FileSaveRestore_Cdnjs_DeleteLibrary()
        {
            _libManConfig.Open();
            string[] expectedFilesAndFolders = new[] {
                "jquery.js",
                "jquery.min.js",
            };

            Editor.Caret.MoveToExpression("\"version\": \"1.0\"");
            Editor.Caret.MoveToEndOfLine();
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"defaultProvider\":");
            LibmanTestsUtility.WaitForCompletionEntry(Editor, "cdnjs", caseInsensitive: true);
            Editor.KeyboardCommands.Type("cdnjs");

            Editor.Caret.MoveToExpression("\"libraries\"");
            Editor.Caret.MoveDown(2);
            Editor.KeyboardCommands.Type("\"library\":");
            LibmanTestsUtility.WaitForCompletionEntry(Editor, "jquery", caseInsensitive: true, timeout: 5000);
            Editor.KeyboardCommands.Type("jquery@3.3.1");
            Editor.KeyboardCommands.Right();
            Editor.KeyboardCommands.Type(",");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"destination\":");
            Editor.KeyboardCommands.Type("wwwroot/lib/jquery");

            _libManConfig.Save();
            string cwd = Path.Combine(Path.GetDirectoryName(_webProject.FullPath), "wwwroot", "lib", "jquery");
            LibmanTestsUtility.WaitForRestoredFiles(cwd, expectedFilesAndFolders, caseInsensitive: true);

            Editor.Caret.MoveToExpression("{", 0, 2);

            for (int i = 0; i < 4; ++i)
            {
                Editor.Edit.DeleteLine();
            }

            _libManConfig.Save();
            cwd = Path.Combine(Path.GetDirectoryName(_webProject.FullPath), "wwwroot", "lib");
            LibmanTestsUtility.WaitForDeletedFile(cwd, "jquery", caseInsensitive: true);
        }

        [TestMethod]
        public void FileSaveRestore_Cdnjs_DeleteFile()
        {
            _libManConfig.Open();
            string[] expectedFilesAndFolders = new[] {
                "jquery.js",
                "jquery.min.js",
            };

            Editor.Caret.MoveToExpression("\"version\": \"1.0\"");
            Editor.Caret.MoveToEndOfLine();
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"defaultProvider\":");
            LibmanTestsUtility.WaitForCompletionEntry(Editor, "cdnjs", caseInsensitive: true);
            Editor.KeyboardCommands.Type("cdnjs");

            Editor.Caret.MoveToExpression("\"libraries\"");
            Editor.Caret.MoveDown(2);
            Editor.KeyboardCommands.Type("\"library\":");
            LibmanTestsUtility.WaitForCompletionEntry(Editor, "jquery", caseInsensitive: true, timeout: 5000);
            Editor.KeyboardCommands.Type("jquery@3.3.1");
            Editor.KeyboardCommands.Right();
            Editor.KeyboardCommands.Type(",");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"destination\":");
            Editor.KeyboardCommands.Type("wwwroot/lib/jquery");

            Editor.KeyboardCommands.Right();
            Editor.KeyboardCommands.Type(",");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"files\": [");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"jquery.js\",");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"jquery.min.js\"");

            _libManConfig.Save();
            string cwd = Path.Combine(Path.GetDirectoryName(_webProject.FullPath), "wwwroot", "lib", "jquery");
            LibmanTestsUtility.WaitForRestoredFiles(cwd, expectedFilesAndFolders, caseInsensitive: true);

            Editor.Edit.DeleteToBeginningOfLine();
            _libManConfig.Save();
            LibmanTestsUtility.WaitForDeletedFile(cwd, "jquery.min.js", caseInsensitive: true);
        }

        [TestMethod]
        public void FileSaveRestore_Unpkg_DeleteLibrary()
        {
            _libManConfig.Open();
            string[] expectedFilesAndFolders = new[] {
                "LICENSE.txt",
                "AUTHORS.txt",
            };

            Editor.Caret.MoveToExpression("\"version\": \"1.0\"");
            Editor.Caret.MoveToEndOfLine();
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"defaultProvider\":");
            LibmanTestsUtility.WaitForCompletionEntry(Editor, "unpkg", caseInsensitive: true);
            Editor.KeyboardCommands.Type("unpkg");

            Editor.Caret.MoveToExpression("\"libraries\"");
            Editor.Caret.MoveDown(2);
            Editor.KeyboardCommands.Type("\"library\":");
            Editor.KeyboardCommands.Type("jquery@3.3.1");
            Editor.KeyboardCommands.Right();
            Editor.KeyboardCommands.Type(",");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"destination\":");
            Editor.KeyboardCommands.Type("wwwroot/lib/jquery");

            _libManConfig.Save();
            string cwd = Path.Combine(Path.GetDirectoryName(_webProject.FullPath), "wwwroot", "lib", "jquery");
            LibmanTestsUtility.WaitForRestoredFiles(cwd, expectedFilesAndFolders, caseInsensitive: true);

            Editor.Caret.MoveToExpression("{", 0, 2);

            for (int i = 0; i < 4; ++i)
            {
                Editor.Edit.DeleteLine();
            }

            _libManConfig.Save();
            LibmanTestsUtility.WaitForDeletedFiles(cwd, expectedFilesAndFolders, caseInsensitive: true);
        }

        [TestMethod]
        public void FileSaveRestore_Unpkg_DeleteFile()
        {
            _libManConfig.Open();
            string[] expectedFilesAndFolders = new[] {
                "LICENSE.txt",
                "AUTHORS.txt",
            };

            Editor.Caret.MoveToExpression("\"version\": \"1.0\"");
            Editor.Caret.MoveToEndOfLine();
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"defaultProvider\":");
            LibmanTestsUtility.WaitForCompletionEntry(Editor, "unpkg", caseInsensitive: true);
            Editor.KeyboardCommands.Type("unpkg");

            Editor.Caret.MoveToExpression("\"libraries\"");
            Editor.Caret.MoveDown(2);
            Editor.KeyboardCommands.Type("\"library\":");
            Editor.KeyboardCommands.Type("jquery@3.3.1");
            Editor.KeyboardCommands.Right();
            Editor.KeyboardCommands.Type(",");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"destination\":");
            Editor.KeyboardCommands.Type("wwwroot/lib/jquery");

            Editor.KeyboardCommands.Right();
            Editor.KeyboardCommands.Type(",");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"files\": [");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"LICENSE.txt\",");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"AUTHORS.txt\"");

            _libManConfig.Save();
            string cwd = Path.Combine(Path.GetDirectoryName(_webProject.FullPath), "wwwroot", "lib", "jquery");
            LibmanTestsUtility.WaitForRestoredFiles(cwd, expectedFilesAndFolders, caseInsensitive: true);

            Editor.Edit.DeleteToBeginningOfLine();
            _libManConfig.Save();
            LibmanTestsUtility.WaitForDeletedFile(cwd, "AUTHORS.txt", caseInsensitive: true);
        }
    }
}
