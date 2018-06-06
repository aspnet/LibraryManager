// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Threading;
using System.IO;
using System.Linq;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal sealed class ManageLibrariesCommand
    {
        private readonly Package _package;

        private ManageLibrariesCommand(Package package, OleMenuCommandService commandService)
        {
            _package = package;

            var cmdId = new CommandID(PackageGuids.guidLibraryManagerPackageCmdSet, PackageIds.ManageLibraries);
            var cmd = new OleMenuCommand(ExecuteAsync, cmdId);
            commandService.AddCommand(cmd);
        }

        public static ManageLibrariesCommand Instance { get; private set; }

        private IServiceProvider ServiceProvider => _package;

        public static void Initialize(Package package, OleMenuCommandService commandService)
        {
            Instance = new ManageLibrariesCommand(package, commandService);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;

            button.Visible = true;
            button.Enabled = KnownUIContexts.SolutionExistsAndNotBuildingAndNotDebuggingContext.IsActive;
        }

        private async void ExecuteAsync(object sender, EventArgs e)
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
                    Manifest manifest = await Manifest.FromFileAsync(configFilePath, dependencies, default(CancellationToken));
                    manifest.DefaultProvider = "cdnjs";
                    manifest.Version = Manifest.SupportedVersions.Max().ToString();

                        await manifest.SaveAsync(configFilePath, default(CancellationToken));
                        await project.AddFileToProjectAsync(configFilePath);

                        Telemetry.TrackUserTask("ConfigFileCreated");
                    }

                    await VsHelpers.OpenFileAsync(configFilePath);
                }
            }
        }
}
