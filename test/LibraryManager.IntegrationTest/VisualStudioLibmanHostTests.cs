using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Test.Apex.VisualStudio;
using Microsoft.Test.Apex.VisualStudio.Editor;
using Microsoft.Test.Apex.VisualStudio.Solution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Web.LibraryManager.IntegrationTest
{
    [TestClass]
    [DeploymentItem(_rootDirectoryName, _rootDirectoryName)]
    public class VisualStudioLibmanHostTest : VisualStudioHostTest
    {
        // Solution consts
        const string _testSolutionName = @"TestSolution.sln";
        const string _rootDirectoryName = @"TestSolution";

        private static VisualStudioLibmanHostTest _instance;
        private static string _resultPath;
        private static string _solutionRootPath;
        private static string _solutionPath;

        protected override void DoHostTestInitialize()
        {
            _instance = this;

            base.DoHostTestInitialize();

            Solution.Open(_solutionPath);
            Solution.WaitForFullyLoaded(); // This will get modified after bug 627108 get fixed
        }

        protected override void DoHostTestCleanup()
        {
            Thread.Sleep(1000); // This can be removed after bug 624281 get fixed

            base.DoHostTestCleanup();
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

        [AssemblyInitialize()]
        public static void AssemblyInit(TestContext context)
        {
            _resultPath = context.DeploymentDirectory;
            _solutionRootPath = Path.Combine(_resultPath, _rootDirectoryName);
            _solutionPath = Path.Combine(_solutionRootPath, _testSolutionName);
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
