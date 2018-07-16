using System;
using System.ComponentModel.Design;
using System.IO;
using System.Threading;
using System.Windows.Interop;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Web.LibraryManager.Contracts;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal sealed class InstallLibraryCommand
    {
        private readonly ILibraryCommandService _libraryCommandService;

        private InstallLibraryCommand(OleMenuCommandService commandService, ILibraryCommandService libraryCommandService)
        {
            CommandID cmdId = new CommandID(PackageGuids.guidLibraryManagerPackageCmdSet, PackageIds.InstallPackage);
            OleMenuCommand cmd = new OleMenuCommand(ExecuteHandlerAsync, cmdId);
            cmd.BeforeQueryStatus += BeforeQueryStatusHandlerAsync;
            commandService.AddCommand(cmd);

            _libraryCommandService = libraryCommandService;
        }

        public static InstallLibraryCommand Instance
        {
            get;
            private set;
        }

        public static void Initialize(Package package, OleMenuCommandService commandService, ILibraryCommandService libraryCommandService)
        {
            Instance = new InstallLibraryCommand(commandService, libraryCommandService);
        }

        private async void BeforeQueryStatusHandlerAsync(object sender, EventArgs e)
        {
            try
            {
                await BeforeQueryStatusAsync(sender, e);
            }
            catch { }
        }

        private async void ExecuteHandlerAsync(object sender, EventArgs e)
        {
            try
            {
                await ExecuteAsync(sender, e);
            }
            catch { }
        }

        private async Task BeforeQueryStatusAsync(object sender, EventArgs e)
        {
            OleMenuCommand button = (OleMenuCommand)sender;
            button.Visible = button.Enabled = false;

            // When command is invooked from a folder
            ProjectItem item = await VsHelpers.GetSelectedItemAsync().ConfigureAwait(false);
            // When command is invoked from project scope
            Project project = await VsHelpers.GetProjectOfSelectedItemAsync().ConfigureAwait(false);

            // We won't enable the command if it was not invoked from a project or a folder
            if (item?.ContainingProject == null && project == null)
            {
                return;
            }

            button.Visible = true;
            button.Enabled = KnownUIContexts.SolutionExistsAndNotBuildingAndNotDebuggingContext.IsActive && !_libraryCommandService.IsOperationInProgress;
        }

        private async Task ExecuteAsync(object sender, EventArgs e)
        {
            Telemetry.TrackUserTask("Execute-InstallLibraryCommand");

            ProjectItem item = await VsHelpers.GetSelectedItemAsync().ConfigureAwait(false);
            Project project = await VsHelpers.GetProjectOfSelectedItemAsync().ConfigureAwait(false);

            if (project != null)
            {
                string rootFolder = await project.GetRootFolderAsync().ConfigureAwait(false);

                string configFilePath = Path.Combine(rootFolder, Constants.ConfigFileName);
                IDependencies dependencies = Dependencies.FromConfigFile(configFilePath);

                Manifest manifest = await Manifest.FromFileAsync(configFilePath, dependencies, CancellationToken.None).ConfigureAwait(false);

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // If the manifest contains errors, we will not invoke the "Add Client-Side libraries" dialog
                // Instead we will display a message box indicating the syntax errors in manifest file.
                if (manifest == null)
                {
                    IVsUIShell shell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;
                    int result;

                    shell.ShowMessageBox(dwCompRole: 0,
                                         rclsidComp: Guid.Empty,
                                         pszTitle: null,
                                         pszText: PredefinedErrors.ManifestMalformed().Message,
                                         pszHelpFile: null,
                                         dwHelpContextID: 0,
                                         msgbtn: OLEMSGBUTTON.OLEMSGBUTTON_OK,
                                         msgdefbtn: OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                                         msgicon: OLEMSGICON.OLEMSGICON_WARNING,
                                         fSysAlert: 0,
                                         pnResult: out result);

                    return;
                }

                string target = string.Empty;

                // Install command was invoked from a folder.
                // So the initial target location should be name of the folder from which
                // the command was invoked.
                if (item != null)
                {
                    target = item.FileNames[1];
                }
                else
                {
                    // Install command was invoked from project scope.
                    // If wwwroot exists, initial target location should be - wwwroot/lib.
                    // Else, target location should be - lib
                    if (Directory.Exists(Path.Combine(rootFolder, "wwwroot")))
                    {
                        target = Path.Combine(rootFolder, "wwwroot", "lib") + Path.DirectorySeparatorChar;
                    }
                    else
                    {
                        target = Path.Combine(rootFolder, "lib") + Path.DirectorySeparatorChar;
                    }
                }

                UI.InstallDialog dialog = new UI.InstallDialog(dependencies, _libraryCommandService, configFilePath, target, rootFolder, project);

                var dte = (DTE)Package.GetGlobalService(typeof(SDTE));
                int hwnd = dte.MainWindow.HWnd;
                WindowInteropHelper windowInteropHelper = new WindowInteropHelper(dialog);

                // Set visual studio window's handle as the owner of the dialog.
                // This will remove the dialog from alt-tab list and will not allow the user to switch the dialog box to the background 
                windowInteropHelper.Owner = new IntPtr(hwnd);

                dialog.ShowDialog();

                Telemetry.TrackUserTask("Open-InstallDialog");
            }
        }
    }
}
