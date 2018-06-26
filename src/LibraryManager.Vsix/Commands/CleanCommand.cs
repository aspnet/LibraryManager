// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Design;
using System.Threading;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

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
            var cmd = new OleMenuCommand(ExecuteHandlerAsync, cmdId);
            cmd.BeforeQueryStatus += BeforeQueryStatusHandlerAsync;
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

        private async void BeforeQueryStatusHandlerAsync(object sender, EventArgs e)
        {
            try
            {
                await BeforeQueryStatusAsync(sender, e);
            }
            catch { }
        }

        private async void ExecuteHandlerAsync(object sender, EventArgs e)
        {
            try
            {
                await ExecuteAsync();
            }
            catch { }
        }

        private async Task BeforeQueryStatusAsync(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            button.Visible = button.Enabled = false;

            ProjectItem item = await VsHelpers.GetSelectedItemAsync();

            if (item != null &&
                item.Name.Equals(Constants.ConfigFileName, StringComparison.OrdinalIgnoreCase))
            {
                button.Visible = true;
                button.Enabled = KnownUIContexts.SolutionExistsAndNotBuildingAndNotDebuggingContext.IsActive && !_libraryCommandService.IsOperationInProgress;
            }
        }

        private async Task ExecuteAsync()
        {
            ProjectItem configProjectItem = await VsHelpers.GetSelectedItemAsync();

            if (configProjectItem != null)
            {
                await _libraryCommandService.CleanAsync(configProjectItem, CancellationToken.None);
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
