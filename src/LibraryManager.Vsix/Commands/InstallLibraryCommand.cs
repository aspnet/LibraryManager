using System;
using System.ComponentModel.Design;
using System.IO;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal sealed class InstallLibraryCommand
    {
        private readonly ILibraryCommandService _libraryCommandService;

        private InstallLibraryCommand(OleMenuCommandService commandService, ILibraryCommandService libraryCommandService)
        {
            CommandID cmdId = new CommandID(PackageGuids.guidLibraryManagerPackageCmdSet, PackageIds.InstallPackage);
            OleMenuCommand cmd = new OleMenuCommand(ExecuteAsync, cmdId);
            cmd.BeforeQueryStatus += BeforeQueryStatus;
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

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand button = (OleMenuCommand)sender;
            button.Visible = button.Enabled = false;

            ProjectItem item = VsHelpers.GetSelectedItem();

            if (item?.ContainingProject == null)
            {
                return;
            }

            if (!_libraryCommandService.IsOperationInProgress && VSConstants.ItemTypeGuid.PhysicalFolder_string.Equals(item.Kind, StringComparison.OrdinalIgnoreCase))
            {
                button.Visible = true;
                button.Enabled = KnownUIContexts.SolutionExistsAndNotBuildingAndNotDebuggingContext.IsActive;
            }
        }

        private void ExecuteAsync(object sender, EventArgs e)
        {
            Telemetry.TrackUserTask("installdialogopened");

            ProjectItem item = VsHelpers.GetSelectedItem();

            if (item != null)
            {
                string target = item.FileNames[1];

                Project project = VsHelpers.GetProjectOfSelectedItem();

                if (project != null)
                {
                    string rootFolder = project.GetRootFolder();

                    string configFilePath = Path.Combine(rootFolder, Constants.ConfigFileName);
                    IDependencies dependencies = Dependencies.FromConfigFile(configFilePath);

                    UI.InstallDialog dialog = new UI.InstallDialog(dependencies, _libraryCommandService, configFilePath, target, rootFolder);
                    dialog.ShowDialog();
                }
            }
        }
    }
}
