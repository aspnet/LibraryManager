// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.Web.LibraryInstaller.Vsix
{
    internal sealed class CleanCommand
    {
        private readonly Package _package;
        private readonly BuildEvents _buildEvents;
        private readonly SolutionEvents _solutionEvents;

        private CleanCommand(Package package, OleMenuCommandService commandService)
        {
            _package = package;

            var cmdId = new CommandID(PackageGuids.guidLibraryInstallerPackageCmdSet, PackageIds.Clean);
            var cmd = new OleMenuCommand(ExecuteAsync, cmdId);
            cmd.BeforeQueryStatus += BeforeQueryStatus;
            commandService.AddCommand(cmd);

            _buildEvents = VsHelpers.DTE.Events.BuildEvents;
            _buildEvents.OnBuildBegin += OnBuildBegin;

            _solutionEvents = VsHelpers.DTE.Events.SolutionEvents;
            _solutionEvents.AfterClosing += AfterClosing;
        }

        public static CleanCommand Instance
        {
            get;
            private set;
        }

        private IServiceProvider ServiceProvider => _package;

        public static void Initialize(Package package, OleMenuCommandService commandService)
        {
            Instance = new CleanCommand(package, commandService);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            button.Visible = button.Enabled = false;

            if (VsHelpers.DTE.SelectedItems.MultiSelect)
                return;

            ProjectItem item = VsHelpers.DTE.SelectedItems.Item(1).ProjectItem;

            if (item.Name.Equals(Constants.ConfigFileName, StringComparison.OrdinalIgnoreCase))
                button.Visible = button.Enabled = true;
        }

        private async void ExecuteAsync(object sender, EventArgs e)
        {
            ProjectItem configProjectItem = VsHelpers.DTE.SelectedItems.Item(1).ProjectItem;

            if (configProjectItem != null)
                await LibraryHelpers.CleanAsync(configProjectItem);

            TelemetryResult result = configProjectItem != null ? TelemetryResult.Success : TelemetryResult.Failure;
            Telemetry.TrackUserTask("clean", result);
        }

        private void OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
        {
            if (Action == vsBuildAction.vsBuildActionClean || Action == vsBuildAction.vsBuildActionRebuildAll)
            {
                // Removes all Library Installer errors from the Error List
                TableDataSource.Instance.CleanAllErrors();
            }
        }

        private void AfterClosing()
        {
            Logger.ClearOutputWindow();
            TableDataSource.Instance.CleanAllErrors();
        }
    }
}
