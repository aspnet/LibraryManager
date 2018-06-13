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
        private string _fileContent;
        private string _pathToLibmanFile;
        private string _pathToLibmanLibraryFile;
        private string _pathToJquery;

        [TestInitialize()]
        public void initialize()
        {
            _webProject = Solution[_projectName];
            _libManConfig = _webProject["libman.json"];
        }

        protected override void DoHostTestInitialize()
        {
            if (string.IsNullOrEmpty(_fileContent))
            {
                _pathToLibmanFile = Path.Combine(SolutionRootPath, _projectName, "libman.json");
                _pathToLibmanLibraryFile = Path.Combine(SolutionRootPath, _projectName, "libman-library.json");
                _fileContent = File.ReadAllText(_pathToLibmanLibraryFile);
                _pathToJquery = Path.Combine(SolutionRootPath, _projectName, "wwwroot", "lib", "jquery");
            }

            // Delete restored libraries
            if (Directory.Exists(_pathToJquery))
            {
                Directory.Delete(_pathToJquery, true);
            }

            // Reverting the libman.json file
            File.WriteAllText(_pathToLibmanFile, _fileContent);

            base.DoHostTestInitialize();
        }

        [TestMethod]
        public void FileSaveRestore_Cdnjs_AddNewLibraryWithoutSpecifingFiles()
        {
            _libManConfig.Open();
            string[] expectedFilesAndFolders = new[] {
                Path.Combine(_pathToJquery, "jquery.js"),
                Path.Combine(_pathToJquery, "jquery.min.js"),
            };

            Editor.Caret.MoveToExpression("\"version\": \"1.0\"");
            Editor.Caret.MoveToEndOfLine();
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"defaultProvider\":");
            LibmanTestsUtility.WaitForCompletionEntry(Editor, "cdnjs", caseInsensitive: true);
            Editor.KeyboardCommands.Type("cdnjs");
            
            _libManConfig.Save();
            LibmanTestsUtility.WaitForRestoredFiles(_pathToJquery, expectedFilesAndFolders, caseInsensitive: true);
        }

        [TestMethod]
        public void FileSaveRestore_Unpkg_AddNewLibraryWithoutSpecifingFiles()
        {
            _libManConfig.Open();
            string[] expectedFilesAndFolders = new[] {
                Path.Combine(_pathToJquery, "LICENSE.txt"),
                Path.Combine(_pathToJquery, "dist", "jquery.js"),
            };

            Editor.Caret.MoveToExpression("\"version\": \"1.0\"");
            Editor.Caret.MoveToEndOfLine();
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"defaultProvider\":");
            LibmanTestsUtility.WaitForCompletionEntry(Editor, "unpkg", caseInsensitive: true);
            Editor.KeyboardCommands.Type("unpkg");

            _libManConfig.Save();
            LibmanTestsUtility.WaitForRestoredFiles(_pathToJquery, expectedFilesAndFolders, caseInsensitive: true);
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

            Editor.Caret.MoveToExpression("\"files\"");
            Editor.Caret.MoveDown();
            Editor.KeyboardCommands.Type("\"jquery.js\"");

            _libManConfig.Save();
            LibmanTestsUtility.WaitForRestoredFile(_pathToJquery, Path.Combine(_pathToJquery, "jquery.js"), caseInsensitive: true);
            LibmanTestsUtility.WaitForRestoredFileNotPresent(_pathToJquery, Path.Combine(_pathToJquery, "jquery.min.js"), caseInsensitive: true);
        }

        [TestMethod]
        public void FileSaveRestore_Unpkg_AddNewLibraryWithSpecifingFiles()
        {
            _libManConfig.Open();
            string[] expectedFilesAndFolders = new[] {
                Path.Combine(_pathToJquery, "dist", "jquery.js"),
                Path.Combine(_pathToJquery, "dist", "jquery.min.js"),
            };

            Editor.Caret.MoveToExpression("\"version\": \"1.0\"");
            Editor.Caret.MoveToEndOfLine();
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"defaultProvider\":");
            LibmanTestsUtility.WaitForCompletionEntry(Editor, "unpkg", caseInsensitive: true);
            Editor.KeyboardCommands.Type("unpkg");

            Editor.Caret.MoveToExpression("\"files\"");
            Editor.Caret.MoveDown();
            Editor.KeyboardCommands.Type("\"dist/jquery.js\",");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"dist/jquery.min.js\"");

            _libManConfig.Save();
            LibmanTestsUtility.WaitForRestoredFiles(_pathToJquery, expectedFilesAndFolders, caseInsensitive: true);
            LibmanTestsUtility.WaitForRestoredFileNotPresent(_pathToJquery, Path.Combine(_pathToJquery, "dist", "jquery.min.map"), caseInsensitive: true);
        }

        [TestMethod]
        public void FileSaveRestore_Cdnjs_DeleteLibrary()
        {
            _libManConfig.Open();
            string[] expectedFilesAndFolders = new[] {
                Path.Combine(_pathToJquery, "jquery.js"),
                Path.Combine(_pathToJquery, "jquery.min.js"),
            };

            Editor.Caret.MoveToExpression("\"version\": \"1.0\"");
            Editor.Caret.MoveToEndOfLine();
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"defaultProvider\":");
            LibmanTestsUtility.WaitForCompletionEntry(Editor, "cdnjs", caseInsensitive: true);
            Editor.KeyboardCommands.Type("cdnjs");

            _libManConfig.Save();
            LibmanTestsUtility.WaitForRestoredFiles(_pathToJquery, expectedFilesAndFolders, caseInsensitive: true);

            Editor.Caret.MoveToExpression("{", 0, 2);

            for (int i = 0; i < 6; ++i)
            {
                Editor.Edit.DeleteLine();
            }

            _libManConfig.Save();
            string pathToLibFolder = Path.Combine(Path.GetDirectoryName(_webProject.FullPath), "wwwroot", "lib");
            LibmanTestsUtility.WaitForDeletedFile(pathToLibFolder, Path.Combine(_pathToJquery, "jquery.js"), caseInsensitive: true);
        }

        [TestMethod]
        public void FileSaveRestore_Unpkg_DeleteLibrary()
        {
            _libManConfig.Open();
            string[] expectedFilesAndFolders = new[] {
                Path.Combine(_pathToJquery, "LICENSE.txt"),
                Path.Combine(_pathToJquery, "dist", "jquery.js"),
            };

            Editor.Caret.MoveToExpression("\"version\": \"1.0\"");
            Editor.Caret.MoveToEndOfLine();
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"defaultProvider\":");
            LibmanTestsUtility.WaitForCompletionEntry(Editor, "unpkg", caseInsensitive: true);
            Editor.KeyboardCommands.Type("unpkg");

            _libManConfig.Save();
            LibmanTestsUtility.WaitForRestoredFiles(_pathToJquery, expectedFilesAndFolders, caseInsensitive: true);

            Editor.Caret.MoveToExpression("{", 0, 2);

            for (int i = 0; i < 6; ++i)
            {
                Editor.Edit.DeleteLine();
            }

            _libManConfig.Save();
            LibmanTestsUtility.WaitForDeletedFiles(_pathToJquery, expectedFilesAndFolders, caseInsensitive: true);
        }

        [TestMethod]
        public void FileSaveRestore_Cdnjs_DeleteFile()
        {
            _libManConfig.Open();
            string[] expectedFilesAndFolders = new[] {
                Path.Combine(_pathToJquery, "jquery.js"),
                Path.Combine(_pathToJquery, "jquery.min.js"),
            };

            Editor.Caret.MoveToExpression("\"version\": \"1.0\"");
            Editor.Caret.MoveToEndOfLine();
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"defaultProvider\":");
            LibmanTestsUtility.WaitForCompletionEntry(Editor, "cdnjs", caseInsensitive: true);
            Editor.KeyboardCommands.Type("cdnjs");

            Editor.Caret.MoveToExpression("\"files\"");
            Editor.Caret.MoveDown();
            Editor.KeyboardCommands.Type("\"jquery.js\",");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"jquery.min.js\"");

            _libManConfig.Save();
            LibmanTestsUtility.WaitForRestoredFiles(_pathToJquery, expectedFilesAndFolders, caseInsensitive: true);

            Editor.Caret.MoveToExpression("jquery.js");
            Editor.Edit.DeleteLine();
            _libManConfig.Save();
            LibmanTestsUtility.WaitForDeletedFile(_pathToJquery, Path.Combine(_pathToJquery, "jquery.js"), caseInsensitive: true);
        }

        [TestMethod]
        public void FileSaveRestore_Unpkg_DeleteFile()
        {
            _libManConfig.Open();
            string[] expectedFilesAndFolders = new[] {
                Path.Combine(_pathToJquery, "dist", "jquery.js"),
                Path.Combine(_pathToJquery, "dist", "jquery.min.js"),
            };

            Editor.Caret.MoveToExpression("\"version\": \"1.0\"");
            Editor.Caret.MoveToEndOfLine();
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"defaultProvider\":");
            LibmanTestsUtility.WaitForCompletionEntry(Editor, "unpkg", caseInsensitive: true);
            Editor.KeyboardCommands.Type("unpkg");

            Editor.Caret.MoveToExpression("\"files\"");
            Editor.Caret.MoveDown();
            Editor.KeyboardCommands.Type("\"dist/jquery.js\",");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"dist/jquery.min.js\"");

            _libManConfig.Save();
            LibmanTestsUtility.WaitForRestoredFiles(_pathToJquery, expectedFilesAndFolders, caseInsensitive: true);

            Editor.Caret.MoveToExpression("dist/jquery.js");
            Editor.Edit.DeleteLine();
            _libManConfig.Save();
            LibmanTestsUtility.WaitForDeletedFile(_pathToJquery, Path.Combine(_pathToJquery, "dist/jquery.js"), caseInsensitive: true);
        }
    }
}
