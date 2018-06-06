// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Design;
using System.Threading;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal sealed class CleanCommand
    {
        private readonly Package _package;
        private readonly BuildEvents _buildEvents;
        private readonly SolutionEvents _solutionEvents;
        private readonly ILibraryCommandService _libraryCommandService;

        private CleanCommand(Package package, OleMenuCommandService commandService, ILibraryCommandService libraryCommandService)
        {
            _package = package;
            _libraryCommandService = libraryCommandService;

            var cmdId = new CommandID(PackageGuids.guidLibraryManagerPackageCmdSet, PackageIds.Clean);
            var cmd = new OleMenuCommand(ExecuteAsync, cmdId);
            cmd.BeforeQueryStatus += BeforeQueryStatus;
            commandService.AddCommand(cmd);

            _buildEvents = VsHelpers.DTE.Events.BuildEvents;
            _buildEvents.OnBuildBegin += OnBuildBegin;

            _solutionEvents = VsHelpers.DTE.Events.SolutionEvents;
            _solutionEvents.AfterClosing += AfterClosing;
        }

        public static CleanCommand Instance { get; private set; }

        private IServiceProvider ServiceProvider => _package;

        public static void Initialize(Package package, OleMenuCommandService commandService, ILibraryCommandService libraryCommandService)
        {
            Instance = new CleanCommand(package, commandService, libraryCommandService);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            button.Visible = button.Enabled = false;

            ProjectItem item = VsHelpers.GetSelectedItem();

            if (!_libraryCommandService.IsOperationInProgress && 
                item != null && 
                item.Name.Equals(Constants.ConfigFileName, StringComparison.OrdinalIgnoreCase))
            {
                button.Visible = true;
                button.Enabled = KnownUIContexts.SolutionExistsAndNotBuildingAndNotDebuggingContext.IsActive;
            }
        }

        private async void ExecuteAsync(object sender, EventArgs e)
        {
            ProjectItem configProjectItem = VsHelpers.GetSelectedItem();

            if (configProjectItem != null)
            {
                await _libraryCommandService.CleanAsync(configProjectItem);
            }
        }

        private void OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
        {
            if (Action == vsBuildAction.vsBuildActionClean || Action == vsBuildAction.vsBuildActionRebuildAll)
            {
                // Removes all Library Manager errors from the Error List
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
