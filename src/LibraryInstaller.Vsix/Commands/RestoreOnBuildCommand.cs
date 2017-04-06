// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using EnvDTE;
using LibraryInstaller.Contracts;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using NuGet.VisualStudio;
using System;
using System.ComponentModel.Design;
using System.Windows;
using System.Windows.Interop;

namespace LibraryInstaller.Vsix
{
    internal sealed class RestoreOnBuildCommand
    {
        private bool _isInstalled;
        IComponentModel _componentModel;

        private RestoreOnBuildCommand(OleMenuCommandService commandService)
        {
            _componentModel = VsHelpers.GetService<SComponentModel, IComponentModel>();

            var cmdId = new CommandID(PackageGuids.guidLibraryInstallerPackageCmdSet, PackageIds.RestoreOnBuild);
            var cmd = new OleMenuCommand(Execute, cmdId);
            cmd.BeforeQueryStatus += BeforeQueryStatus;
            commandService.AddCommand(cmd);
        }

        public static RestoreOnBuildCommand Instance
        {
            get;
            private set;
        }

        public static void Initialize(Package package, OleMenuCommandService commandService)
        {
            Instance = new RestoreOnBuildCommand(commandService);
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

            if (item.IsConfigFile())
            {
                button.Visible = button.Enabled = true;

                _isInstalled = IsPackageInstalled(item.ContainingProject);
                button.Checked = _isInstalled;

                if (_isInstalled)
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

            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    if (!_isInstalled)
                    {
                        MessageBoxResult question = MessageBox.Show(Resources.Text.NugetInstallPrompt, Vsix.Name, MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (question == MessageBoxResult.No)
                            return;

                        Logger.LogEvent("Installing NuGet package containing MSBuild target...", LogLevel.Status);

                        IVsPackageInstaller2 installer = _componentModel.GetService<IVsPackageInstaller2>();
                        installer.InstallLatestPackage(null, item.ContainingProject, Constants.NuGetPackageId, true, false);

                        Telemetry.TrackUserTask("InstallNugetPackage");
                        Logger.LogEvent("NuGet package installed", LogLevel.Status);
                    }
                    else
                    {
                        Logger.LogEvent("Uninstalling NuGet package...", LogLevel.Status);

                        IVsPackageUninstaller uninstaller = _componentModel.GetService<IVsPackageUninstaller>();
                        uninstaller.UninstallPackage(item.ContainingProject, Constants.NuGetPackageId, false);

                        Telemetry.TrackUserTask("UninstallNugetPackage");
                        Logger.LogEvent("NuGet package uninstalled", LogLevel.Status);
                    }
                }
                catch (Exception ex)
                {
                    Telemetry.TrackException(nameof(RestoreOnBuildCommand), ex);
                    Logger.LogEvent("Error installing NuGet package", LogLevel.Status);
                }
            });
        }

        private bool IsPackageInstalled(Project project)
        {
            IVsPackageInstallerServices installerServices = _componentModel.GetService<IVsPackageInstallerServices>();

            return installerServices.IsPackageInstalled(project, Constants.NuGetPackageId);
        }
    }
}
