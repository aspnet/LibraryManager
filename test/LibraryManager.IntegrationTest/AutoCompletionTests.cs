using System.IO;
using System.Reflection;
using Microsoft.Test.Apex.VisualStudio;
using Microsoft.Test.Apex.VisualStudio.Solution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Web.LibraryManager.IntegrationTest
{
    [TestClass]
    public class AutoCompletionTests : VisualStudioLibmanHostTest
    {
        [TestMethod]
        public void AutoCompletion_ProvidePathForDestinationProperty()
        {
            ProjectTestExtension webProject;
            string projectName = "WebApplication1";

            webProject = Solution.ProjectsRecursive[projectName];

            ProjectItemTestExtension libManConfig = webProject.Find(SolutionItemFind.FileName, "libman.json");
            libManConfig.Open();
            string[] expectedCompletionEntries = new [] {
                "Properties/",
                "wwwroot/"
            };

            Editor.Caret.MoveDown(4);
            Editor.Caret.MoveToEndOfLine();
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"destination\":");

            string errorMessage = LibmanTestsUtility.WaitForCompletionEntries(Editor, expectedCompletionEntries, caseInsensitive: true, timeout: 10000);
            Assert.IsTrue(string.IsNullOrEmpty(errorMessage), errorMessage);
        }
    }
}
