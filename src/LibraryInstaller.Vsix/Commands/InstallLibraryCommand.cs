// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using EnvDTE;
using LibraryInstaller.Contracts;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Threading;

namespace LibraryInstaller.Vsix
{
    internal sealed class InstallLibraryCommand
    {
        private readonly Package _package;

        private InstallLibraryCommand(Package package, OleMenuCommandService commandService)
        {
            _package = package;

            var cmdId = new CommandID(PackageGuids.guidLibraryInstallerPackageCmdSet, PackageIds.InstallPackage);
            var cmd = new OleMenuCommand(ExecuteAsync, cmdId);
            cmd.BeforeQueryStatus += BeforeQueryStatus;
            commandService.AddCommand(cmd);
        }

        public static InstallLibraryCommand Instance
        {
            get;
            private set;
        }

        private IServiceProvider ServiceProvider => _package;

        public static void Initialize(Package package, OleMenuCommandService commandService)
        {
            Instance = new InstallLibraryCommand(package, commandService);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            button.Visible = button.Enabled = false;

            ProjectItem item = VsHelpers.DTE.SelectedItems.Item(1)?.ProjectItem;

            if (item?.ContainingProject == null || !item.ContainingProject.IsSupported())
                return;

            if (item.Kind.Equals(VSConstants.ItemTypeGuid.PhysicalFolder_string, StringComparison.OrdinalIgnoreCase))
                button.Visible = button.Enabled = true;
        }

        private async void ExecuteAsync(object sender, EventArgs e)
        {
            Telemetry.TrackUserTask("installdialogopened");

            CancellationToken token = CancellationToken.None;
            Project project = VsHelpers.DTE.SelectedItems.Item(1).ProjectItem.ContainingProject;
            string rootFolder = project.GetRootFolder();

            string configFilePath = Path.Combine(rootFolder, Constants.ConfigFileName);
            var dependencies = Dependencies.FromConfigFile(configFilePath);
            Manifest manifest = await Manifest.FromFileAsync(configFilePath, dependencies, token);

            string itemFolder = VsHelpers.DTE.SelectedItems.Item(1).ProjectItem.Properties.Item("FullPath").Value.ToString();
            string relativeFolder = PackageUtilities.MakeRelative(rootFolder, itemFolder).Replace('\\', '/').Trim('/');
            ILibraryInstallationState state = GetLibraryToInstall(relativeFolder);
            manifest.AddLibrary(state);
            await manifest.SaveAsync(configFilePath, token);

            project.AddFileToProject(configFilePath);
            await LibraryHelpers.RestoreAsync(configFilePath);
        }

        private ILibraryInstallationState GetLibraryToInstall(string relativeFolderPath)
        {
            // TODO: Implement UI that returs the installation state object
            return new LibraryInstallationState
            {
                LibraryId = "jquery@3.1.1",
                ProviderId = "cdnjs",
                Path = relativeFolderPath,
                Files = new[] { "jquery.js", "jquery.min.js" }
            };
        }
    }
}
