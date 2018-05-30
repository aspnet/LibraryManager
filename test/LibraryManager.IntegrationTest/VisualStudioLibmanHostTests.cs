using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.Test.Apex.VisualStudio;
using Microsoft.Test.Apex.VisualStudio.Editor;
using Microsoft.Test.Apex.VisualStudio.Solution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Web.LibraryManager.IntegrationTest
{
    [TestClass]
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
            Solution.WaitForFullyLoaded();
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

            // Deploy the TestSolution folder
            FilesDeployer.DeployDirectory(Assembly.GetExecutingAssembly(), _resultPath, _rootDirectoryName);
        }

        [AssemblyCleanup()]
        public static void AssemblyCleanup()
        {
            if (!string.IsNullOrEmpty(_solutionRootPath))
            {
                FilesDeployer.ForceDeleteDirectory(_solutionRootPath);
            }

            try
            {
                if (_instance != null)
                {
                    Process visualStudioProcess = _instance.VisualStudio.HostProcess;

                    if (_instance.VisualStudio.ObjectModel.Solution.IsOpen)
                    {
                        _instance.VisualStudio.ObjectModel.Solution.Close();
                    }

                    if (!visualStudioProcess.HasExited)
                    {
                        visualStudioProcess.Kill();
                    }
                }
            }
            catch (Exception) { }
        }
    }
}
