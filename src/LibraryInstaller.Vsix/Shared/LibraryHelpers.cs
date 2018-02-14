// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using EnvDTE;
using Microsoft.Web.LibraryManager.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal static class LibraryHelpers
    {
        public static bool IsSupported(this Project project)
        {
            if (project.IsKind(ProjectTypes.DOTNET_Core, ProjectTypes.WEBSITE_PROJECT))
                return true;

            try
            {
                // Web Application Project has this property
                if (project.Properties.Item("WebApplication.AspNetDebugging") != null)
                    return true;
            }
            catch
            { /* Do nothing. If property doesn't exist, it throws. */ }

            return false;
        }

        public static async Task RestoreAsync(string configFilePath, CancellationToken cancellationToken = default(CancellationToken))
        {
            await RestoreAsync(new[] { configFilePath }, cancellationToken).ConfigureAwait(false);
        }

        public static async Task RestoreAsync(IEnumerable<string> configFilePaths, CancellationToken cancellationToken = default(CancellationToken))
        {
            Logger.LogEvent(LibraryManager.Resources.Text.RestoringLibraries, LogLevel.Status);

            var sw = new Stopwatch();
            sw.Start();
            int resultCount = 0;
            bool hasErrors = false;
            var telResult = new Dictionary<string, double>();

            foreach (string configFilePath in configFilePaths)
            {
                IEnumerable<ILibraryInstallationResult> results = await RestoreLibrariesAsync(configFilePath, cancellationToken).ConfigureAwait(false);
                Project project = VsHelpers.DTE.Solution?.FindProjectItem(configFilePath)?.ContainingProject;
                AddFilesToProject(configFilePath, project, results);

                var errorList = new ErrorList(project?.Name, configFilePath);
                hasErrors |= errorList.HandleErrors(results);

                resultCount += results.Count();

                foreach (ILibraryInstallationResult result in results)
                {
                    telResult.TryGetValue(result.InstallationState.ProviderId, out double count);
                    telResult[result.InstallationState.ProviderId] = count + 1;
                }
            }

            sw.Stop();

            telResult.Add("time", sw.Elapsed.TotalMilliseconds);
            Telemetry.TrackUserTask("restore", telResult.Select(i => new KeyValuePair<string, object>(i.Key, i.Value)).ToArray());

            if (resultCount > 0)
            {
                string text = hasErrors ?
                    LibraryManager.Resources.Text.RestoreHasErrors :
                    string.Format(LibraryManager.Resources.Text.LibrariesRestored, resultCount, Math.Round(sw.Elapsed.TotalSeconds, 2));

                Logger.LogEvent(Environment.NewLine + text + Environment.NewLine, LogLevel.Task);
            }
            else
            {
                Logger.LogEvent(Environment.NewLine + Resources.Text.LibraryRestoredNoChange + Environment.NewLine, LogLevel.Task);
            }
        }

        public static async Task UninstallAsync(string configFilePath, string libraryId, CancellationToken cancellationToken)
        {
            var dependencies = Dependencies.FromConfigFile(configFilePath);
            Manifest manifest = await Manifest.FromFileAsync(configFilePath, dependencies, cancellationToken).ConfigureAwait(false);
            var hostInteraction = dependencies.GetHostInteractions() as HostInteraction;
            manifest.Uninstall(libraryId, (file) => hostInteraction.DeleteFiles(file));

            Telemetry.TrackUserTask("libraryuninstall");
        }

        public static async Task CleanAsync(ProjectItem configProjectItem)
        {
            Logger.LogEvent(Resources.Text.CleanLibrariesStarted, LogLevel.Task);

            string configFileName = configProjectItem.FileNames[1];
            var dependencies = Dependencies.FromConfigFile(configFileName);
            Manifest manifest = await Manifest.FromFileAsync(configFileName, dependencies, CancellationToken.None).ConfigureAwait(false);
            var hostInteraction = dependencies.GetHostInteractions() as HostInteraction;

            int? filesDeleted = manifest?.Clean((file) => hostInteraction.DeleteFiles(file));

            Logger.LogEvent(Resources.Text.CleanLibrariesSucceeded + Environment.NewLine, LogLevel.Task);

            TelemetryResult result = configProjectItem != null ? TelemetryResult.Success : TelemetryResult.Failure;
            Telemetry.TrackUserTask("clean", new KeyValuePair<string, object>("filesdeleted", filesDeleted));
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
                    IEnumerable<string> absoluteFiles = state.InstallationState.Files
                        .Select(file => Path.Combine(cwd, state.InstallationState.DestinationPath, file)
                        .Replace('/', Path.DirectorySeparatorChar));
                    files.AddRange(absoluteFiles.Where(file => !files.Contains(file)));
                }
            }

            project?.AddFilesToProject(files);
        }
    }
}
