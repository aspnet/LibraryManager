using System;
using System.ComponentModel.Design;
using System.IO;
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
            Telemetry.TrackUserTask("installdialogopened");

            ProjectItem item = await VsHelpers.GetSelectedItemAsync().ConfigureAwait(false);
            Project project = await VsHelpers.GetProjectOfSelectedItemAsync().ConfigureAwait(false);

            if (project != null)
            {
                string target = string.Empty;
                string rootFolder = await project.GetRootFolderAsync().ConfigureAwait(false);

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

                string configFilePath = Path.Combine(rootFolder, Constants.ConfigFileName);
                IDependencies dependencies = Dependencies.FromConfigFile(configFilePath);

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                UI.InstallDialog dialog = new UI.InstallDialog(dependencies, _libraryCommandService, configFilePath, target, rootFolder);

                var dte = (DTE)Package.GetGlobalService(typeof(SDTE));
                int hwnd = dte.MainWindow.HWnd;
                WindowInteropHelper windowInteropHelper = new WindowInteropHelper(dialog);

                // Set visual studio window's handle as the owner of the dialog.
                // This will remove the dialog from alt-tab list and will not allow the user to switch the dialog box to the background 
                windowInteropHelper.Owner = new IntPtr(hwnd);

                dialog.ShowDialog();
            }
        }
    }
}
