// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using EnvDTE;
using LibraryInstaller.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryInstaller.Vsix
{
    internal static class LibraryHelpers
    {
        public static bool IsSupported(this Project project)
        {
            return project.IsKind(ProjectTypes.WAP, ProjectTypes.DOTNET_Core, ProjectTypes.WEBSITE_PROJECT);
        }

        public static async Task RestoreAsync(string configFilePath, CancellationToken cancellationToken = default(CancellationToken))
        {
            await RestoreAsync(new[] { configFilePath }, cancellationToken);
        }

        public static async Task RestoreAsync(IEnumerable<string> configFilePaths, CancellationToken cancellationToken = default(CancellationToken))
        {
            Logger.LogEvent(Resources.Text.RestoringLibraries, Level.Status);

            var sw = new Stopwatch();
            sw.Start();
            int fileCount = 0;
            bool hasErrors = false;

            foreach (string configFilePath in configFilePaths)
            {
                IEnumerable<ILibraryInstallationResult> result = await RestoreLibrariesAsync(configFilePath, cancellationToken);
                Project project = VsHelpers.DTE.Solution?.FindProjectItem(configFilePath)?.ContainingProject;
                AddFilesToProject(configFilePath, project, result);

                var errorList = new ErrorList(project?.Name, configFilePath);
                hasErrors |= errorList.HandleErrors(result);

                fileCount += result.Count();
            }

            sw.Stop();

            if (fileCount > 0)
            {
                string text = hasErrors ?
                    Resources.Text.RestoreHasErrors :
                    string.Format(Resources.Text.LibrariesRestored, fileCount, Math.Round(sw.Elapsed.TotalSeconds, 2));

                Logger.LogEvent(Environment.NewLine + text + Environment.NewLine, Level.Task);
            }
            else
            {
                Logger.LogEvent(Environment.NewLine + Resources.Text.LibraryRestoredNoChange + Environment.NewLine, Level.Task);
            }
        }

        public static async Task CleanAsync(ProjectItem configProjectItem)
        {
            Logger.LogEvent(Resources.Text.CleanLibrariesStarted, Level.Task);

            string configFileName = configProjectItem.FileNames[1];
            var dependencies = Dependencies.FromConfigFile(configFileName);
            Manifest manifest = await Manifest.FromFileAsync(configFileName, dependencies, CancellationToken.None);

            manifest?.Clean();

            Logger.LogEvent(Resources.Text.CleanLibrariesSucceeded + Environment.NewLine, Level.Task);
        }

        private static async Task<IEnumerable<ILibraryInstallationResult>> RestoreLibrariesAsync(string configFilePath, CancellationToken cancellationToken)
        {
            var dependencies = Dependencies.FromConfigFile(configFilePath);

            Manifest manifest = await Manifest.FromFileAsync(configFilePath, dependencies, cancellationToken).ConfigureAwait(false);
            return await manifest.RestoreAsync(cancellationToken).ConfigureAwait(false);
        }

        private static void AddFilesToProject(string configFilePath, Project project, IEnumerable<ILibraryInstallationResult> results)
        {
            string cwd = Path.GetDirectoryName(configFilePath);
            var files = new List<string>();

            foreach (ILibraryInstallationResult state in results)
            {
                if (state.Success)
                {
                    IEnumerable<string> absoluteFiles = state.InstallationState.Files.Select(file => Path.Combine(cwd, state.InstallationState.Path, file).Replace('/', '\\'));
                    files.AddRange(absoluteFiles.Where(file => !files.Contains(file)));
                }
            }

            if (project != null)
                project.AddFilesToProject(files);
        }
    }
}
