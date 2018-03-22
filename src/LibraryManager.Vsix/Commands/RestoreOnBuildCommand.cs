// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using EnvDTE;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio;
using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Collections.Generic;

namespace Microsoft.Web.LibraryManager.Vsix
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

            var cmdId = new CommandID(PackageGuids.guidLibraryManagerPackageCmdSet, PackageIds.RestoreOnBuild);
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
                button.Visible = true;
                button.Enabled = KnownUIContexts.SolutionExistsAndNotBuildingAndNotDebuggingContext.IsActive;

                _isPackageInstalled = IsPackageInstalled(item.ContainingProject);

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
                var dependencies = Dependencies.FromConfigFile(item.FileNames[1]);
                IEnumerable<string> packageIds = dependencies.Providers.Select(p => p.NuGetPackageId).Distinct();

                if (!_isPackageInstalled)
                {
                    if (!UserWantsToInstall())
                        return;

                    System.Threading.Tasks.Task.Run(() =>
                    {
                        Logger.LogEvent("Installing NuGet package containing MSBuild target...", LogLevel.Status);

                        try
                        {
                            foreach (string packageId in packageIds)
                            {
                                IVsPackageInstaller2 installer = _componentModel.GetService<IVsPackageInstaller2>();
                                installer.InstallLatestPackage(null, item.ContainingProject, packageId, true, false);
                            }

                            Telemetry.TrackUserTask("InstallNugetPackage");
                            Logger.LogEvent("NuGet package installed", LogLevel.Status);
                        }
                        catch (Exception ex)
                        {
                            Telemetry.TrackException(nameof(RestoreOnBuildCommand), ex);
                            Logger.LogEvent("NuGet package failed to install", LogLevel.Status);
                        }
                    });
                }
                else
                {
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        Logger.LogEvent("Uninstalling NuGet package...", LogLevel.Status);

                        try
                        {
                            foreach (string packageId in packageIds)
                            {
                                IVsPackageUninstaller uninstaller = _componentModel.GetService<IVsPackageUninstaller>();
                                uninstaller.UninstallPackage(item.ContainingProject, packageId, false);
                            }

                            Telemetry.TrackUserTask("UninstallNugetPackage");
                            Logger.LogEvent("NuGet package uninstalled", LogLevel.Status);
                        }            
                        catch (Exception ex)
                        {
                            Telemetry.TrackException(nameof(RestoreOnBuildCommand), ex);
                            Logger.LogEvent("NuGet package failed to uninstall", LogLevel.Status);
                        }
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

            return installerServices.IsPackageInstalled(project, Constants.MainNuGetPackageId);
        }
    }
}
