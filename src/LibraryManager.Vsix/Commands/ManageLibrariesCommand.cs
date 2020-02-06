// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Threading;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.Web.LibraryManager.Vsix.Contracts;
using Microsoft.Web.LibraryManager.Vsix.Shared;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Web.LibraryManager.Vsix.Commands
{
    internal sealed class ManageLibrariesCommand
    {
        private readonly ILibraryCommandService _libraryCommandService;
        private readonly IDependenciesFactory _dependenciesFactory;

        private ManageLibrariesCommand(AsyncPackage package, OleMenuCommandService commandService, ILibraryCommandService libraryCommandService, IDependenciesFactory dependenciesFactory)
        {
            _libraryCommandService = libraryCommandService;
            _dependenciesFactory = dependenciesFactory;

            var cmdId = new CommandID(PackageGuids.guidLibraryManagerPackageCmdSet, PackageIds.ManageLibraries);
            var cmd = new OleMenuCommand((s, e) => package.JoinableTaskFactory.RunAsync(() => ExecuteAsync(s, e)),
                                         cmdId);
            cmd.BeforeQueryStatus += BeforeQueryStatus;
            commandService.AddCommand(cmd);
        }

        public static ManageLibrariesCommand Instance { get; private set; }

        public static void Initialize(AsyncPackage package, OleMenuCommandService commandService, ILibraryCommandService libraryCommandService, IDependenciesFactory dependenciesFactory)
        {
            Instance = new ManageLibrariesCommand(package, commandService, libraryCommandService, dependenciesFactory);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;

            button.Visible = true;
            button.Enabled = KnownUIContexts.SolutionExistsAndNotBuildingAndNotDebuggingContext.IsActive && !_libraryCommandService.IsOperationInProgress;
        }

        private async Task ExecuteAsync(object sender, EventArgs e)
        {
            Telemetry.TrackUserTask("Execute-ManageLibrariesCommand");

            Project project = await VsHelpers.GetProjectOfSelectedItemAsync();

            if (project != null)
            {
                string rootFolder = await project.GetRootFolderAsync();

                string configFilePath = Path.Combine(rootFolder, Constants.ConfigFileName);

                if (File.Exists(configFilePath))
                {
                    await VsHelpers.OpenFileAsync(configFilePath);
                }
                else
                {
                    var dependencies = _dependenciesFactory.FromConfigFile(configFilePath);
                    Manifest manifest = await Manifest.FromFileAsync(configFilePath, dependencies, CancellationToken.None);
                    manifest.DefaultProvider = "cdnjs";
                    manifest.Version = Manifest.SupportedVersions.Max().ToString();

                    await manifest.SaveAsync(configFilePath, CancellationToken.None);
                    await project.AddFileToProjectAsync(configFilePath);

                    Telemetry.TrackUserTask("Create-ConfigFile");
                }

                await VsHelpers.OpenFileAsync(configFilePath);
            }
        }
    }
}
