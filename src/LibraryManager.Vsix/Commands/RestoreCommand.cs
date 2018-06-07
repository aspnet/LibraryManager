// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal sealed class RestoreCommand
    {
        private readonly Package _package;
        private readonly ILibraryCommandService _libraryCommandService;

        private RestoreCommand(Package package, OleMenuCommandService commandService, ILibraryCommandService libraryCommandService)
        {
            _package = package;
            _libraryCommandService = libraryCommandService;

            var cmdId = new CommandID(PackageGuids.guidLibraryManagerPackageCmdSet, PackageIds.Restore);
            var cmd = new OleMenuCommand(ExecuteAsync, cmdId);
            cmd.BeforeQueryStatus += BeforeQueryStatus;
            commandService.AddCommand(cmd);
        }

        public static RestoreCommand Instance { get; private set; }

        private IServiceProvider ServiceProvider => _package;

        public static void Initialize(Package package, OleMenuCommandService commandService, ILibraryCommandService libraryCommandService)
        {
            Instance = new RestoreCommand(package, commandService, libraryCommandService);
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
            ProjectItem configProjectItem = VsHelpers.GetSelectedItem();

            if (!_libraryCommandService.IsOperationInProgress && configProjectItem != null)
            {
                await _libraryCommandService.RestoreAsync(configProjectItem.FileNames[1] );
            }
        }
    }
}
