// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Vsix.Contracts;
using Microsoft.Web.LibraryManager.Vsix.Shared;
using NuGet.VisualStudio;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Web.LibraryManager.Vsix.Commands
{
    internal sealed class RestoreOnBuildCommand
    {
        private bool _isPackageInstalled;
        private readonly IComponentModel _componentModel;
        private readonly AsyncPackage _package;
        private readonly IDependenciesFactory _dependenciesFactory;

        private RestoreOnBuildCommand(AsyncPackage package, OleMenuCommandService commandService, IDependenciesFactory dependenciesFactory)
        {
            _package = package;
            _componentModel = VsHelpers.GetService<SComponentModel, IComponentModel>();
            _dependenciesFactory = dependenciesFactory;

            var cmdId = new CommandID(PackageGuids.guidLibraryManagerPackageCmdSet, PackageIds.RestoreOnBuild);
            var cmd = new OleMenuCommand((s, e) => _package.JoinableTaskFactory.RunAsync(() => ExecuteAsync(s, e)),
                                         cmdId);
            cmd.BeforeQueryStatus += (s, e) => _package.JoinableTaskFactory.RunAsync(() => BeforeQueryStatusAsync(s, e));
            commandService.AddCommand(cmd);
        }

        public static RestoreOnBuildCommand Instance { get; private set; }

        private IServiceProvider ServiceProvider
        {
            get { return _package; }
        }

        public static void Initialize(AsyncPackage package, OleMenuCommandService commandService, IDependenciesFactory dependenciesFactory)
        {
            Instance = new RestoreOnBuildCommand(package, commandService, dependenciesFactory);
        }

        private async Task BeforeQueryStatusAsync(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            button.Visible = button.Enabled = false;

            if (VsHelpers.DTE.SelectedItems.MultiSelect)
            {
                return;
            }

            ProjectItem item = await VsHelpers.GetSelectedItemAsync();

            if (item != null && item.IsConfigFile())
            {
                button.Visible = true;
                button.Enabled = KnownUIContexts.SolutionExistsAndNotBuildingAndNotDebuggingContext.IsActive;

                _isPackageInstalled = IsPackageInstalled(item.ContainingProject);

                if (_isPackageInstalled)
                {
                    button.Text = Resources.Text.DisableRestoreOnBuild;
                }
                else
                {
                    button.Text = Resources.Text.EnableRestoreOnBuild;
                }
            }
        }

        private async Task ExecuteAsync(object sender, EventArgs e)
        {
            ProjectItem projectItem = await VsHelpers.GetSelectedItemAsync();
            Project project = await VsHelpers.GetProjectOfSelectedItemAsync();

            try
            {
                var dependencies = _dependenciesFactory.FromConfigFile(projectItem.get_FileNames(1));
                IEnumerable<string> packageIds = dependencies.Providers
                                                             .Where(p => p.NuGetPackageId != null)
                                                             .Select(p => p.NuGetPackageId)
                                                             .Distinct();

                if (!_isPackageInstalled)
                {
                    if (!UserWantsToInstall())
                        return;

                    await Task.Run(() =>
                    {
                        Logger.LogEvent(Resources.Text.Nuget_InstallingPackage, LogLevel.Status);

                        try
                        {
                            foreach (string packageId in packageIds)
                            {
                                IVsPackageInstaller2 installer = _componentModel.GetService<IVsPackageInstaller2>();
                                installer.InstallLatestPackage(null, project, packageId, true, false);
                            }

                            Telemetry.TrackUserTask("Install-NugetPackage");
                            Logger.LogEvent(Resources.Text.Nuget_PackageInstalled, LogLevel.Status);
                        }
                        catch (Exception ex)
                        {
                            Telemetry.TrackException(nameof(RestoreOnBuildCommand), ex);
                            Logger.LogEvent(Resources.Text.Nuget_PackageFailedToInstall, LogLevel.Status);
                        }
                    });
                }
                else
                {
                    await Task.Run(() =>
                    {
                        Logger.LogEvent(Resources.Text.Nuget_UninstallingPackage, LogLevel.Status);

                        try
                        {
                            foreach (string packageId in packageIds)
                            {
                                IVsPackageUninstaller uninstaller = _componentModel.GetService<IVsPackageUninstaller>();
                                uninstaller.UninstallPackage(project, packageId, false);
                            }

                            Telemetry.TrackUserTask("Uninstall-NugetPackage");
                            Logger.LogEvent(Resources.Text.Nuget_PackageUninstalled, LogLevel.Status);
                        }            
                        catch (Exception ex)
                        {
                            Telemetry.TrackException(nameof(RestoreOnBuildCommand), ex);
                            Logger.LogEvent(Resources.Text.Nuget_PackageFailedToUninstall, LogLevel.Status);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Telemetry.TrackException(nameof(RestoreOnBuildCommand), ex);
                Logger.LogEvent(Resources.Text.Nuget_PackageFailedToInstall, LogLevel.Status);
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
