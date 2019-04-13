// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
    [DeploymentItem(RootDirectoryName, RootDirectoryName)]
    public class VisualStudioLibmanHostTest : VisualStudioHostTest
    {
        // Solution consts
        protected const string LibmanJsonFileName = "libman.json";
        protected const string ProjectName = @"TestProjectCore20";
        private const string RootDirectoryName = @"TestSolution";
        private const string TestSolutionName = @"TestSolution.sln";

        protected ProjectItemTestExtension _libmanConfig;
        protected string _pathToLibmanFile;
        protected ProjectTestExtension _webProject;
        private string _initialLibmanFileContent;
        private static VisualStudioLibmanHostTest Instance;
        private static string ResultPath;
        private static string SolutionPath;

        public static string SolutionRootPath { get; private set; }

        protected HelperWrapper Helpers { get; private set; }

        protected override void DoHostTestInitialize()
        {
            Instance = this;

            base.DoHostTestInitialize();

            Helpers = new HelperWrapper();

            Solution.Open(SolutionPath);
            Solution.WaitForFullyLoaded(); // This will get modified after bug 627108 get fixed

            _webProject = Solution[ProjectName];
            _libmanConfig = _webProject[LibmanJsonFileName];
            _pathToLibmanFile = Path.Combine(SolutionRootPath, ProjectName, LibmanJsonFileName);
            _initialLibmanFileContent = File.ReadAllText(_pathToLibmanFile);
        }

        protected override void DoHostTestCleanup()
        {
            ProjectTestExtension webProject = Solution[ProjectName];
            ProjectItemTestExtension libmanConfig = webProject[LibmanJsonFileName];

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
            var guid = Guid.Parse("44ee7bda-abda-486e-a5fe-4dd3f4cefac1");
            uint commandId = 0x0200;
            SolutionExplorerItemTestExtension libmanConfigNode = SolutionExplorer.FindItemRecursive(LibmanJsonFileName);

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
            DocumentWindowTestExtension doc = _libmanConfig.Open();
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
            ResultPath = context.DeploymentDirectory;
            SolutionRootPath = Path.Combine(ResultPath, RootDirectoryName);
            SolutionPath = Path.Combine(SolutionRootPath, TestSolutionName);
        }

        [AssemblyCleanup()]
        public static void AssemblyCleanup()
        {
            try
            {
                if (Instance != null)
                {
                    Process visualStudioProcess = Instance.VisualStudio.HostProcess;

                    if (Instance.VisualStudio.ObjectModel.Solution.IsOpen)
                    {
                        Instance.VisualStudio.ObjectModel.Solution.Close();
                    }

                    PostMessage(Instance.VisualStudio.MainWindowHandle, 0x10, IntPtr.Zero, IntPtr.Zero); // WM_CLOSE
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
