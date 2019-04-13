using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Test.Apex.Services;
using Microsoft.Test.Apex.VisualStudio;
using Microsoft.Test.Apex.VisualStudio.Editor;
using Microsoft.Test.Apex.VisualStudio.Shell;
using Microsoft.Test.Apex.VisualStudio.Shell.ToolWindows;
using Microsoft.Test.Apex.VisualStudio.Solution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.IntegrationTest.Helpers;

namespace Microsoft.Web.LibraryManager.IntegrationTest
{
    [TestClass]
    [DeploymentItem(_rootDirectoryName, _rootDirectoryName)]
    public class VisualStudioLibmanHostTest : VisualStudioHostTest
    {
        // Solution consts
        protected const string _libman = "libman.json";
        protected const string _projectName = @"TestProjectCore20";
        private const string _rootDirectoryName = @"TestSolution";
        private const string _testSolutionName = @"TestSolution.sln";

        protected ProjectItemTestExtension _libmanConfig;
        protected string _pathToLibmanFile;
        protected ProjectTestExtension _webProject;
        private string _initialLibmanFileContent;
        private static VisualStudioLibmanHostTest _instance;
        private static string _resultPath;
        private static string _solutionPath;

        public static string SolutionRootPath { get; private set; }

        protected HelperWrapper Helpers { get; private set; }

        protected override void DoHostTestInitialize()
        {
            _instance = this;

            base.DoHostTestInitialize();

            Helpers = new HelperWrapper(VisualStudio);

            Solution.Open(_solutionPath);
            Solution.WaitForFullyLoaded(); // This will get modified after bug 627108 get fixed

            _webProject = Solution[_projectName];
            _libmanConfig = _webProject[_libman];
            _pathToLibmanFile = Path.Combine(SolutionRootPath, _projectName, _libman);
            _initialLibmanFileContent = File.ReadAllText(_pathToLibmanFile);
        }

        protected override void DoHostTestCleanup()
        {
            ProjectTestExtension webProject = Solution[_projectName];
            ProjectItemTestExtension libmanConfig = webProject[_libman];

            if (libmanConfig != null)
            {
                CleanClientSideLibraries();

                libmanConfig.Open();
                Editor.Selection.SelectAll();
                Editor.KeyboardCommands.Delete();
                Editor.Edit.InsertTextInBuffer(_initialLibmanFileContent);

                libmanConfig.Save();
            }

            base.DoHostTestCleanup();
        }

        private void CleanClientSideLibraries()
        {
            Guid guid = Guid.Parse("44ee7bda-abda-486e-a5fe-4dd3f4cefac1");
            uint commandId = 0x0200;
            SolutionExplorerItemTestExtension libmanConfigNode = SolutionExplorer.FindItemRecursive(_libman);

            if (libmanConfigNode != null)
            {
                libmanConfigNode.Select();

                WaitFor.IsTrue(() =>
                {
                    CommandQueryResult queryResult = VisualStudio.ObjectModel.Commanding.QueryStatusCommand(guid, commandId);
                    return queryResult.IsEnabled;
                }, TimeSpan.FromMilliseconds(40000), TimeSpan.FromMilliseconds(500));

                VisualStudio.ObjectModel.Commanding.ExecuteCommand(guid, commandId, null);
            }
        }

        /// <summary>
        /// Sets the contents of the manifest file by editing it in VS
        /// </summary>
        protected void SetManifestContents(string contents)
        {
            var doc = _libmanConfig.Open();
            Editor.Selection.SelectAll();
            Editor.KeyboardCommands.Delete();
            Editor.Edit.InsertTextInBuffer(contents);
            doc.Save();
        }

        protected override VisualStudioHostConfiguration GetVisualStudioHostConfiguration()
        {
            VisualStudioHostConfiguration configuration = base.GetVisualStudioHostConfiguration();

            // start the experimental instance
            configuration.CommandLineArguments += " /RootSuffix Exp";

            return configuration;
        }

        public IVisualStudioTextEditorTestExtension Editor
        {
            get
            {
                return VisualStudio.ObjectModel.WindowManager.ActiveDocumentWindowAsTextEditor.Editor;
            }
        }

        public SolutionService Solution
        {
            get
            {
                return VisualStudio.ObjectModel.Solution;
            }
        }

        internal SolutionExplorerService SolutionExplorer
        {
            get
            {
                return VisualStudio.ObjectModel.Shell.ToolWindows.SolutionExplorer;
            }
        }

        [AssemblyInitialize()]
        public static void AssemblyInit(TestContext context)
        {
            _resultPath = context.DeploymentDirectory;
            SolutionRootPath = Path.Combine(_resultPath, _rootDirectoryName);
            _solutionPath = Path.Combine(SolutionRootPath, _testSolutionName);
        }

        [AssemblyCleanup()]
        public static void AssemblyCleanup()
        {
            try
            {
                if (_instance != null)
                {
                    Process visualStudioProcess = _instance.VisualStudio.HostProcess;

                    if (_instance.VisualStudio.ObjectModel.Solution.IsOpen)
                    {
                        _instance.VisualStudio.ObjectModel.Solution.Close();
                    }

                    PostMessage(_instance.VisualStudio.MainWindowHandle, 0x10, IntPtr.Zero, IntPtr.Zero); // WM_CLOSE
                    visualStudioProcess.WaitForExit(5000);

                    if (!visualStudioProcess.HasExited)
                    {
                        visualStudioProcess.Kill();
                    }
                }
            }
            catch (Exception) { }
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    }
}
