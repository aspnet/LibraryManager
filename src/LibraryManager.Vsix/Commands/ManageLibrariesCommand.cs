// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Threading;
using System.IO;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal sealed class ManageLibrariesCommand
    {
        private readonly Package _package;

        private ManageLibrariesCommand(Package package, OleMenuCommandService commandService)
        {
            _package = package;

            var cmdId = new CommandID(PackageGuids.guidLibraryManagerPackageCmdSet, PackageIds.ManageLibraries);
            var cmd = new OleMenuCommand(Execute, cmdId);
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

        private void Execute(object sender, EventArgs e)
        {
            Telemetry.TrackUserTask("ManageLibraries");
            Project project = VsHelpers.DTE.SelectedItems.Item(1).Project;
            string rootFolder = project.GetRootFolder();

            string configFilePath = Path.Combine(rootFolder, Constants.ConfigFileName);

            if (File.Exists(configFilePath))
            {
                VsHelpers.DTE.ItemOperations.OpenFile(configFilePath);
            }
            else
            {
                System.Threading.Tasks.Task.Run(async () =>
                {
                    CancellationToken token = CancellationToken.None;

                    if (!File.Exists(configFilePath))
                    {
                        var dependencies = Dependencies.FromConfigFile(configFilePath);
                        Manifest manifest = await Manifest.FromFileAsync(configFilePath, dependencies, token);
                        manifest.DefaultProvider = "cdnjs";

                        await manifest.SaveAsync(configFilePath, token);
                        project.AddFileToProject(configFilePath);

                        Telemetry.TrackUserTask("ConfigFileCreated");
                    }

                    VsHelpers.DTE.ItemOperations.OpenFile(configFilePath);
                });
            }
        }
    }
}
