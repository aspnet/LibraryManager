// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal sealed class RestoreSolutionCommand
    {
        private readonly Package _package;
        private readonly ILibraryCommandService _libraryCommandService;

        private RestoreSolutionCommand(Package package, OleMenuCommandService commandService, ILibraryCommandService libraryCommandService)
        {
            _package = package;
            _libraryCommandService = libraryCommandService;

            var cmdId = new CommandID(PackageGuids.guidLibraryManagerPackageCmdSet, PackageIds.RestoreSolution);
            var cmd = new OleMenuCommand(ExecuteAsync, cmdId);
            cmd.BeforeQueryStatus += BeforeQueryStatus;
            commandService.AddCommand(cmd);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            button.Visible = button.Enabled = false;

            if (VsHelpers.DTE.SelectedItems.MultiSelect)
                return;

            var solution = (IVsSolution)ServiceProvider.GetService(typeof(SVsSolution));

            if (!_libraryCommandService.IsOperationInProgress && VsHelpers.SolutionContainsManifestFile(solution))
            {
                button.Visible = true;
                button.Enabled = KnownUIContexts.SolutionExistsAndNotBuildingAndNotDebuggingContext.IsActive;
            }
        }

        public static RestoreSolutionCommand Instance { get; private set; }

        private IServiceProvider ServiceProvider => _package;

        public static void Initialize(Package package, OleMenuCommandService commandService, ILibraryCommandService libraryCommandService)
        {
            Instance = new RestoreSolutionCommand(package, commandService, libraryCommandService);
        }

        private async void ExecuteAsync(object sender, EventArgs e)
        {
            var solution = (IVsSolution)ServiceProvider.GetService(typeof(SVsSolution));
            IEnumerable<IVsHierarchy> hierarchies = VsHelpers.GetProjectsInSolution(solution, __VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION);
            var configFiles = new List<string>();

            foreach (IVsHierarchy hierarchy in hierarchies)
            {
                Project project = VsHelpers.GetDTEProject(hierarchy);

                if (VsHelpers.ProjectContainsManifestFile(project))
                {
                    string configFilePath = Path.Combine(project.GetRootFolder(), Constants.ConfigFileName);
                    configFiles.Add(configFilePath);
                }
            }

            await _libraryCommandService.RestoreAsync(configFiles);

            Telemetry.TrackUserTask("restoresolution");
        }

    }
}
