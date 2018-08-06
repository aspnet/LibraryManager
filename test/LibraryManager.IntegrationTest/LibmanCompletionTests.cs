using System.Collections.Generic;
using Microsoft.Test.Apex.Editor;
using Microsoft.Test.Apex.VisualStudio.Solution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Web.LibraryManager.IntegrationTest
{
    [TestClass]
    public class LibmanCompletionTests : VisualStudioLibmanHostTest
    {
        ProjectItemTestExtension _libManConfig;

        [TestInitialize()]
        public void initialize()
        {
            string projectName = "TestProjectCore20";

            ProjectTestExtension webProject = Solution.ProjectsRecursive[projectName];
            _libManConfig = webProject.Find(SolutionItemFind.FileName, "libman.json");
        }

        [TestMethod]
        public void LibmanCompletion_Destination()
        {
            _libManConfig.Open();
            string[] expectedCompletionEntries = new[] {
                "Properties/",
                "wwwroot/",
            };

            Editor.Caret.MoveToExpression("\"libraries\"");
            Editor.Caret.MoveDown(2);
            Editor.KeyboardCommands.Type("\"destination\":");

            Helpers.Completion.WaitForCompletionEntries(Editor, expectedCompletionEntries, caseInsensitive: true);
        }

        [TestMethod]
        public void LibmanCompletion_Provider()
        {
            _libManConfig.Open();

            string[] expectedCompletionEntries = new[] {
                "cdnjs",
                "filesystem",
                "unpkg",
            };

            Editor.Caret.MoveToExpression("\"libraries\"");
            Editor.Caret.MoveDown(2);
            Editor.KeyboardCommands.Type("\"provider\":");

            Helpers.Completion.WaitForCompletionEntries(Editor, expectedCompletionEntries, caseInsensitive: true);
        }

        [TestMethod]
        public void LibmanCompletion_DefaultProvider()
        {
            _libManConfig.Open();

            string[] expectedCompletionEntries = new[] {
                "cdnjs",
                "filesystem",
                "unpkg",
            };

            Editor.Caret.MoveToExpression("\"version\": \"1.0\"");
            Editor.Caret.MoveToEndOfLine();
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"defaultProvider\":");

            Helpers.Completion.WaitForCompletionEntries(Editor, expectedCompletionEntries, caseInsensitive: true);
        }

        [TestMethod]
        public void LibmanCompletion_DefaultDestination()
        {
            _libManConfig.Open();
            string[] expectedCompletionEntries = new[] {
                "Properties/",
                "wwwroot/",
            };

            Editor.Caret.MoveToExpression("\"version\": \"1.0\"");
            Editor.Caret.MoveToEndOfLine();
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"defaultDestination\":");

            Helpers.Completion.WaitForCompletionEntries(Editor, expectedCompletionEntries, caseInsensitive: true);
        }

        [TestMethod]
        public void LibmanCompletion_LibraryForCdnjs()
        {
            _libManConfig.Open();
            string[] expectedCompletionEntries = new[] {
                "jquery",
            };

            Editor.Caret.MoveToExpression("\"libraries\"");
            Editor.Caret.MoveDown(2);
            Editor.KeyboardCommands.Type("\"provider\": \"cdnjs\",");
            Editor.KeyboardCommands.Enter();

            Editor.KeyboardCommands.Type("\"library\":");
            Helpers.Completion.WaitForCompletionEntries(Editor, expectedCompletionEntries, caseInsensitive: true, timeout: 5000);
        }

        [TestMethod]
        public void LibmanCompletion_LibraryVersionForCdnjs()
        {
            _libManConfig.Open();

            Editor.Caret.MoveToExpression("\"libraries\"");
            Editor.Caret.MoveDown(2);
            Editor.KeyboardCommands.Type("\"provider\": \"cdnjs\",");
            Editor.KeyboardCommands.Enter();

            Editor.KeyboardCommands.Type("\"library\":");
            Helpers.Completion.WaitForCompletionEntry(Editor, "jquery", caseInsensitive: true, timeout: 5000);

            Editor.KeyboardCommands.Type("jquery@");
            Helpers.Completion.WaitForCompletionEntries(Editor, new string[] { }, caseInsensitive: true);
        }

        [TestMethod]
        public void LibmanCompletion_LibraryForFilesystem()
        {
            _libManConfig.Open();
            string[] expectedCompletionEntries = new[] {
                "Properties/",
                "wwwroot/",
            };

            Editor.Caret.MoveToExpression("\"libraries\"");
            Editor.Caret.MoveDown(2);
            Editor.KeyboardCommands.Type("\"provider\": \"filesystem\",");
            Editor.KeyboardCommands.Enter();

            Editor.KeyboardCommands.Type("\"library\":");
            Helpers.Completion.WaitForCompletionEntries(Editor, expectedCompletionEntries, caseInsensitive: true);
        }

        [TestMethod]
        public void LibmanCompletion_LibraryForUnpkg()
        {
            _libManConfig.Open();
            string[] expectedCompletionEntries = new[] {
                "bootstrap",
                "jquery",
            };

            Editor.Caret.MoveToExpression("\"libraries\"");
            Editor.Caret.MoveDown(2);
            Editor.KeyboardCommands.Type("\"provider\": \"unpkg\",");
            Editor.KeyboardCommands.Enter();

            Editor.KeyboardCommands.Type("\"library\":");
            Helpers.Completion.WaitForCompletionEntries(Editor, expectedCompletionEntries, caseInsensitive: true, timeout: 5000);
        }

        [TestMethod]
        public void LibmanCompletion_CompletionForBackSpace()
        {
            _libManConfig.Open();

            Editor.Caret.MoveToExpression("\"libraries\"");
            Editor.Caret.MoveDown(2);
            Editor.KeyboardCommands.Type("\"provider\": \"cdnjs\",");
            Editor.KeyboardCommands.Enter();

            Editor.KeyboardCommands.Type("\"library\":");
            Editor.KeyboardCommands.Type("jque");

            Editor.KeyboardCommands.Backspace();
            Helpers.Completion.WaitForCompletionEntries(Editor, new string[] { }, caseInsensitive: true);
        }

        [TestMethod]
        public void LibmanCompletion_VersionCompletionInDescendingOrder()
        {
            _libManConfig.Open();

            Editor.Caret.MoveToExpression("\"libraries\"");
            Editor.Caret.MoveDown(2);
            Editor.KeyboardCommands.Type("\"provider\": \"unpkg\",");
            Editor.KeyboardCommands.Enter();

            Editor.KeyboardCommands.Type("\"library\":");
            Editor.KeyboardCommands.Type("jquery@");

            CompletionList items = Helpers.Completion.WaitForCompletionItems(Editor, 5000);
            Assert.IsNotNull(items, "Time out waiting for the version completion list");

            List<SemanticVersion> semanticVersions = new List<SemanticVersion>();

            // CompletionList implements the List, so foreach can guarentee its iteration order as original.
            foreach (CompletionItem item in items)
            {
                semanticVersions.Add(SemanticVersion.Parse(item.Text));
            }

            for (int i= 1; i < semanticVersions.Count; ++i)
            {
                Assert.IsTrue(semanticVersions[i].CompareTo(semanticVersions[i - 1]) <= 0);
            }
        }
    }
}
