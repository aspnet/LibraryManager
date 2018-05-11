// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal sealed class RestoreCommand
    {
        private readonly Package _package;

        private RestoreCommand(Package package, OleMenuCommandService commandService)
        {
            _package = package;

            var cmdId = new CommandID(PackageGuids.guidLibraryManagerPackageCmdSet, PackageIds.Restore);
            var cmd = new OleMenuCommand(ExecuteAsync, cmdId);
            cmd.BeforeQueryStatus += BeforeQueryStatus;
            commandService.AddCommand(cmd);
        }

        public static RestoreCommand Instance { get; private set; }

        private IServiceProvider ServiceProvider => _package;

        public static void Initialize(Package package, OleMenuCommandService commandService)
        {
            Instance = new RestoreCommand(package, commandService);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            button.Visible = button.Enabled = false;

            if (VsHelpers.DTE.SelectedItems.MultiSelect)
                return;

            ProjectItem item = VsHelpers.GetSelectedItem();

            if (item != null && item.Name.Equals(Constants.ConfigFileName, StringComparison.OrdinalIgnoreCase))
            {
                button.Visible = true;
                button.Enabled = KnownUIContexts.SolutionExistsAndNotBuildingAndNotDebuggingContext.IsActive;
            }
        }

        private async void ExecuteAsync(object sender, EventArgs e)
        {
            ProjectItem configProjectItem = VsHelpers.GetSelectedItem(); ;

            if (configProjectItem != null)
                await LibraryHelpers.RestoreAsync(configProjectItem.FileNames[1]);
        }
    }
}
