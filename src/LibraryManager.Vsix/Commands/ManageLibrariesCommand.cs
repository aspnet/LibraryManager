// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Threading;
using System.IO;
using System.Linq;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal sealed class ManageLibrariesCommand
    {
        private readonly Package _package;
        private readonly ILibraryCommandService _libraryCommandService;

        private ManageLibrariesCommand(Package package, OleMenuCommandService commandService, ILibraryCommandService libraryCommandService)
        {
            _package = package;
            _libraryCommandService = libraryCommandService;

            var cmdId = new CommandID(PackageGuids.guidLibraryManagerPackageCmdSet, PackageIds.ManageLibraries);
            var cmd = new OleMenuCommand(ExecuteHandlerAsync, cmdId);
            cmd.BeforeQueryStatus += BeforeQueryStatus;
            commandService.AddCommand(cmd);
        }

        public static ManageLibrariesCommand Instance { get; private set; }

        private IServiceProvider ServiceProvider => _package;

        public static void Initialize(Package package, OleMenuCommandService commandService, ILibraryCommandService libraryCommandService)
        {
            Instance = new ManageLibrariesCommand(package, commandService, libraryCommandService);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;

            button.Visible = true;
            button.Enabled = KnownUIContexts.SolutionExistsAndNotBuildingAndNotDebuggingContext.IsActive && !_libraryCommandService.IsOperationInProgress;
        }

        private async void ExecuteHandlerAsync(object sender, EventArgs e)
        {
            try
            {
                await ExecuteAsync(sender, e);
            }
            catch { }
        }

        private async Task ExecuteAsync(object sender, EventArgs e)
        {
            Telemetry.TrackUserTask("ManageLibraries");

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
                    var dependencies = Dependencies.FromConfigFile(configFilePath);
                    Manifest manifest = await Manifest.FromFileAsync(configFilePath, dependencies, CancellationToken.None);
                    manifest.DefaultProvider = "cdnjs";
                    manifest.Version = Manifest.SupportedVersions.Max().ToString();

                    await manifest.SaveAsync(configFilePath, CancellationToken.None);
                    await project.AddFileToProjectAsync(configFilePath);

                    Telemetry.TrackUserTask("ConfigFileCreated");
                    }

                    await VsHelpers.OpenFileAsync(configFilePath);
                }
            }
        }
}
