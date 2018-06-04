using System.Threading;
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

            Editor.Caret.MoveToBeginningOfFile();
            Editor.Caret.MoveDown(4);
            Editor.KeyboardCommands.Type("\"destination\":");

            LibmanTestsUtility.WaitForCompletionEntries(Editor, expectedCompletionEntries, caseInsensitive: true);
        }
    }
}
