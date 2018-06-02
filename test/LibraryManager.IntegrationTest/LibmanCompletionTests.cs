using System.Threading;
using Microsoft.Test.Apex.VisualStudio.Solution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Web.LibraryManager.IntegrationTest
{
    [TestClass]
    public class LibmanCompletionTests : VisualStudioLibmanHostTest
    {
        [TestMethod]
        public void LibCompletion_ProvidePathForDestinationProperty()
        {
            ProjectTestExtension webProject;
            string projectName = "TestProjectCore20";

            webProject = Solution.ProjectsRecursive[projectName];

            ProjectItemTestExtension libManConfig = webProject.Find(SolutionItemFind.FileName, "libman.json");
            libManConfig.Open();
            string[] expectedCompletionEntries = new [] {
                "Properties/",
                "wwwroot/",
            };

            Editor.Caret.MoveToBeginningOfFile();
            Editor.Caret.MoveDown(4);
            Editor.Caret.MoveToEndOfLine();
            Editor.KeyboardCommands.Enter();
            Editor.KeyboardCommands.Type("\"destination\":");

            LibmanTestsUtility.WaitForCompletionEntries(Editor, expectedCompletionEntries, caseInsensitive: true);
        }
    }
}
