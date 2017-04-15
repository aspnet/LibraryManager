// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using EnvDTE;
using Microsoft.Web.LibraryInstaller.Contracts;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio;
using System;
using System.ComponentModel.Design;

namespace Microsoft.Web.LibraryInstaller.Vsix
{
    internal sealed class RestoreOnBuildCommand
    {
        private bool _isPackageInstalled;
        private readonly IComponentModel _componentModel;
        private readonly Package _package;

        private RestoreOnBuildCommand(Package package, OleMenuCommandService commandService)
        {
            _package = package;
            _componentModel = VsHelpers.GetService<SComponentModel, IComponentModel>();

            var cmdId = new CommandID(PackageGuids.guidLibraryInstallerPackageCmdSet, PackageIds.RestoreOnBuild);
            var cmd = new OleMenuCommand(Execute, cmdId);
            cmd.BeforeQueryStatus += BeforeQueryStatus;
            commandService.AddCommand(cmd);
        }

        public static RestoreOnBuildCommand Instance { get; private set; }

        private IServiceProvider ServiceProvider
        {
            get { return _package; }
        }

        public static void Initialize(Package package, OleMenuCommandService commandService)
        {
            Instance = new RestoreOnBuildCommand(package, commandService);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            button.Visible = button.Enabled = false;

            if (VsHelpers.DTE.SelectedItems.MultiSelect)
            {
                return;
            }

            ProjectItem item = VsHelpers.DTE.SelectedItems.Item(1).ProjectItem;

            if (item.IsConfigFile() && item.ContainingProject.IsKind(ProjectTypes.DOTNET_Core, ProjectTypes.WAP))
            {
                button.Visible = button.Enabled = true;

                _isPackageInstalled = IsPackageInstalled(item.ContainingProject);
                button.Checked = _isPackageInstalled;

                if (_isPackageInstalled)
                {
                    button.Text = "Disable Restore on Build";
                }
                else
                {
                    button.Text = "Enable Restore on Build...";
                }
            }
        }

        private void Execute(object sender, EventArgs e)
        {
            ProjectItem item = VsHelpers.DTE.SelectedItems.Item(1).ProjectItem;

            try
            {
                if (!_isPackageInstalled)
                {
                    if (!UserWantsToInstall())
                        return;

                    System.Threading.Tasks.Task.Run(() =>
                    {
                        Logger.LogEvent("Installing NuGet package containing MSBuild target...", LogLevel.Status);

                        IVsPackageInstaller2 installer = _componentModel.GetService<IVsPackageInstaller2>();
                        installer.InstallLatestPackage(null, item.ContainingProject, Constants.NuGetPackageId, true, false);

                        Telemetry.TrackUserTask("InstallNugetPackage");
                        Logger.LogEvent("NuGet package installed", LogLevel.Status);
                    });
                }
                else
                {
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        Logger.LogEvent("Uninstalling NuGet package...", LogLevel.Status);

                        IVsPackageUninstaller uninstaller = _componentModel.GetService<IVsPackageUninstaller>();
                        uninstaller.UninstallPackage(item.ContainingProject, Constants.NuGetPackageId, false);

                        Telemetry.TrackUserTask("UninstallNugetPackage");
                        Logger.LogEvent("NuGet package uninstalled", LogLevel.Status);
                    });
                }
            }
            catch (Exception ex)
            {
                Telemetry.TrackException(nameof(RestoreOnBuildCommand), ex);
                Logger.LogEvent("Error installing NuGet package", LogLevel.Status);
            }
        }

        private bool UserWantsToInstall()
        {
            int answer = VsShellUtilities.ShowMessageBox(
                        ServiceProvider,
                        Resources.Text.NugetInstallPrompt,
                        Vsix.Name,
                        OLEMSGICON.OLEMSGICON_INFO,
                        OLEMSGBUTTON.OLEMSGBUTTON_YESNO,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST
                    );

            return answer == 6; // 6 = Yes
        }

        private bool IsPackageInstalled(Project project)
        {
            IVsPackageInstallerServices installerServices = _componentModel.GetService<IVsPackageInstallerServices>();

            return installerServices.IsPackageInstalled(project, Constants.NuGetPackageId);
        }
    }
}
