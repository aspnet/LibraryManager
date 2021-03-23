// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Design;
using System.Threading;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.Web.LibraryManager.Vsix.Shared;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Web.LibraryManager.Vsix.Commands
{
    internal sealed class RestoreCommand
    {
        private readonly ILibraryCommandService _libraryCommandService;

        private RestoreCommand(AsyncPackage package, OleMenuCommandService commandService, ILibraryCommandService libraryCommandService)
        {
            _libraryCommandService = libraryCommandService;

            var cmdId = new CommandID(PackageGuids.guidLibraryManagerPackageCmdSet, PackageIds.Restore);
            var cmd = new OleMenuCommand((s, e) => package.JoinableTaskFactory.RunAsync(() => ExecuteAsync(s, e)),
                                         cmdId);
            cmd.BeforeQueryStatus += (s, e) => package.JoinableTaskFactory.RunAsync(() => BeforeQueryStatusAsync(s, e));
            commandService.AddCommand(cmd);
        }

        public static RestoreCommand Instance { get; private set; }

        public static void Initialize(AsyncPackage package, OleMenuCommandService commandService, ILibraryCommandService libraryCommandService)
        {
            Instance = new RestoreCommand(package, commandService, libraryCommandService);
        }

        private async Task BeforeQueryStatusAsync(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            button.Visible = button.Enabled = false;

            if (VsHelpers.DTE.SelectedItems.MultiSelect)
                return;

            ProjectItem item = await VsHelpers.GetSelectedItemAsync();

            if (item != null && item.Name.Equals(Constants.ConfigFileName, StringComparison.OrdinalIgnoreCase))
            {
                button.Visible = true;
                button.Enabled = KnownUIContexts.SolutionExistsAndNotBuildingAndNotDebuggingContext.IsActive && !_libraryCommandService.IsOperationInProgress;
            }
        }

        private async Task ExecuteAsync(object sender, EventArgs e)
        {
            Telemetry.TrackUserTask("Execute-RestoreCommand");

            ProjectItem configProjectItem = await VsHelpers.GetSelectedItemAsync();

            if (configProjectItem != null)
            {
                await _libraryCommandService.RestoreAsync(configProjectItem.get_FileNames(1), CancellationToken.None);
            }
        }
    }
}
