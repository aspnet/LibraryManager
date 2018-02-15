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
        private InstallLibraryCommand(OleMenuCommandService commandService)
        {
            CommandID cmdId = new CommandID(PackageGuids.guidLibraryManagerPackageCmdSet, PackageIds.InstallPackage);
            OleMenuCommand cmd = new OleMenuCommand(ExecuteAsync, cmdId);
            cmd.BeforeQueryStatus += BeforeQueryStatus;
            commandService.AddCommand(cmd);
        }

        public static InstallLibraryCommand Instance
        {
            get;
            private set;
        }

        public static void Initialize(Package package, OleMenuCommandService commandService)
        {
            Instance = new InstallLibraryCommand(commandService);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand button = (OleMenuCommand)sender;
            button.Visible = button.Enabled = false;

            ProjectItem item = VsHelpers.DTE.SelectedItems.Item(1)?.ProjectItem;

            if (item?.ContainingProject == null || !item.ContainingProject.IsSupported())
            {
                return;
            }

            if (item.Kind.Equals(VSConstants.ItemTypeGuid.PhysicalFolder_string, StringComparison.OrdinalIgnoreCase))
            {
                button.Visible = button.Enabled = true;
            }
        }

        private void ExecuteAsync(object sender, EventArgs e)
        {
            Telemetry.TrackUserTask("installdialogopened");

            ProjectItem item = VsHelpers.DTE.SelectedItems.Item(1).ProjectItem;
            string target = item.FileNames[1];

            Project project = VsHelpers.DTE.SelectedItems.Item(1).ProjectItem.ContainingProject;
            string rootFolder = project.GetRootFolder();

            string configFilePath = Path.Combine(rootFolder, Constants.ConfigFileName);
            IDependencies dependencies = Dependencies.FromConfigFile(configFilePath);

            UI.InstallDialog dialog = new UI.InstallDialog(dependencies, configFilePath, target);
            dialog.ShowDialog();
        }
    }
}
