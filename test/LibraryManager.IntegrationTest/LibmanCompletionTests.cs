using System.Collections.Generic;
using Microsoft.Test.Apex.Editor;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Web.LibraryManager.IntegrationTest
{
    [TestClass]
    public class LibmanCompletionTests : VisualStudioLibmanHostTest
    {
        [TestMethod]
        public void LibmanCompletion_Destination()
        {
            _libmanConfig.Open();
            string[] expectedCompletionEntries = new[] {
                "Properties/",
                "wwwroot/",
            };

            Editor.Caret.MoveToExpression("\"libraries\"");
            Editor.Caret.MoveDown(1);
            Editor.KeyboardCommands.Type("{");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"destination\":");

            Helpers.Completion.WaitForCompletionEntries(Editor, expectedCompletionEntries, caseInsensitive: true);
        }

        [TestMethod]
        public void LibmanCompletion_Provider()
        {
            _libmanConfig.Open();

            string[] expectedCompletionEntries = new[] {
                "cdnjs",
                "filesystem",
                "unpkg",
            };

            Editor.Caret.MoveToExpression("\"libraries\"");
            Editor.Caret.MoveDown(1);
            Editor.KeyboardCommands.Type("{");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"provider\":");

            Helpers.Completion.WaitForCompletionEntries(Editor, expectedCompletionEntries, caseInsensitive: true);
        }

        [TestMethod]
        public void LibmanCompletion_DefaultProvider()
        {
            _libmanConfig.Open();

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
            _libmanConfig.Open();
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
            _libmanConfig.Open();
            string[] expectedCompletionEntries = new[] {
                "jquery",
            };

            Editor.Caret.MoveToExpression("\"libraries\"");
            Editor.Caret.MoveDown(1);
            Editor.KeyboardCommands.Type("{");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"provider\": \"cdnjs\",");
            Editor.KeyboardCommands.Enter();

            Editor.KeyboardCommands.Type("\"library\":");
            Helpers.Completion.WaitForCompletionEntries(Editor, expectedCompletionEntries, caseInsensitive: true, timeout: 5000);
        }

        [TestMethod]
        public void LibmanCompletion_LibraryVersionForCdnjs()
        {
            _libmanConfig.Open();

            Editor.Caret.MoveToExpression("\"libraries\"");
            Editor.Caret.MoveDown(1);
            Editor.KeyboardCommands.Type("{");
            Editor.KeyboardCommands.Enter();
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
            _libmanConfig.Open();
            string[] expectedCompletionEntries = new[] {
                "Properties/",
                "wwwroot/",
            };

            Editor.Caret.MoveToExpression("\"libraries\"");
            Editor.Caret.MoveDown(1);
            Editor.KeyboardCommands.Type("{");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"provider\": \"filesystem\",");
            Editor.KeyboardCommands.Enter();

            Editor.KeyboardCommands.Type("\"library\":");
            Helpers.Completion.WaitForCompletionEntries(Editor, expectedCompletionEntries, caseInsensitive: true);
        }

        [TestMethod]
        public void LibmanCompletion_LibraryForUnpkg()
        {
            // This test needs to be updated once we fix https://github.com/aspnet/LibraryManager/issues/221
            _libmanConfig.Open();

            Editor.Caret.MoveToExpression("\"libraries\"");
            Editor.Caret.MoveDown(1);
            Editor.KeyboardCommands.Type("{");
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"provider\": \"unpkg\",");
            Editor.KeyboardCommands.Enter();

            Editor.KeyboardCommands.Type("\"library\":");
            Editor.KeyboardCommands.Type("bootstr");
            Helpers.Completion.WaitForCompletionEntries(Editor, new[] { "bootstrap" }, caseInsensitive: true, timeout: 5000);

            Editor.KeyboardCommands.Backspace(7);
            Editor.KeyboardCommands.Type("jqu");
            Helpers.Completion.WaitForCompletionEntries(Editor, new[] { "jquery" }, caseInsensitive: true, timeout: 5000);
        }

        [TestMethod]
        public void LibmanCompletion_CompletionForBackSpace()
        {
            _libmanConfig.Open();

            Editor.Caret.MoveToExpression("\"libraries\"");
            Editor.Caret.MoveDown(1);
            Editor.KeyboardCommands.Type("{");
            Editor.KeyboardCommands.Enter();
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
            _libmanConfig.Open();

            Editor.Caret.MoveToExpression("\"libraries\"");
            Editor.Caret.MoveDown(1);
            Editor.KeyboardCommands.Type("{");
            Editor.KeyboardCommands.Enter();
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

            for (int i = 1; i < semanticVersions.Count; ++i)
            {
                Assert.IsTrue(semanticVersions[i].CompareTo(semanticVersions[i - 1]) <= 0);
            }
        }
    }
}
