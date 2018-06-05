// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal static class LibraryHelpers
    {
        public static async Task RestoreAsync(string configFilePath, CancellationToken cancellationToken = default(CancellationToken))
        {
            Dependencies dependencies = Dependencies.FromConfigFile(configFilePath);
            Manifest manifest = await Manifest.FromFileAsync(configFilePath, dependencies, cancellationToken).ConfigureAwait(false);

            await RestoreAsync(new Dictionary<string, Manifest>() { [configFilePath] = manifest }, cancellationToken).ConfigureAwait(false);
        }

        public static async Task RestoreAsync(string configFilePath, Manifest manifest, CancellationToken cancellationToken = default(CancellationToken))
        {
            await RestoreAsync(new Dictionary<string, Manifest>() { [configFilePath] = manifest } , cancellationToken).ConfigureAwait(false);
        }

        public static async Task RestoreAsync(IEnumerable<string> configFilePaths, CancellationToken cancellationToken = default(CancellationToken))
        {
            Dictionary<string, Manifest> manifests = new Dictionary<string, Manifest>();

            foreach (string configFilePath in configFilePaths)
            {
                Dependencies dependencies = Dependencies.FromConfigFile(configFilePath);
                Manifest manifest = await Manifest.FromFileAsync(configFilePath, dependencies, cancellationToken).ConfigureAwait(false);

                manifests.Add(configFilePath, manifest);
            }

            await RestoreAsync(manifests, cancellationToken).ConfigureAwait(false);
        }

        private static async Task RestoreAsync(IDictionary<string, Manifest> manifests, CancellationToken cancellationToken = default(CancellationToken))
        {
            Logger.LogEventsHeader(OperationType.Restore);

            Stopwatch sw = new Stopwatch();
            List<ILibraryInstallationResult> totalResults = new List<ILibraryInstallationResult>();

            sw.Start();

            foreach (KeyValuePair<string, Manifest> manifest in manifests)
            {
                Project project = VsHelpers.GetDTEProjectFromConfig(manifest.Key);
                Logger.LogEvent(string.Format(LibraryManager.Resources.Text.Restore_LibrariesForProject, project.FullName), LogLevel.Operation);

                IEnumerable<ILibraryInstallationResult> results = await RestoreLibrariesAsync(manifest.Value, cancellationToken).ConfigureAwait(false);

                await AddFilesToProjectAsync(manifest.Key, project, results, cancellationToken);
                AddErrorsToErrorList(project?.Name, manifest.Key, results);

                totalResults.AddRange(results);
            }

            sw.Stop();

            Logger.LogEventsSummary(totalResults, OperationType.Restore, sw.Elapsed);
            PostRestoreTelemetryData(totalResults, sw.Elapsed);
        }

        private static void PostRestoreTelemetryData(IEnumerable<ILibraryInstallationResult> results, TimeSpan elapsedTime)
        {
            var telResult = new Dictionary<string, double>();
            foreach (ILibraryInstallationResult result in results.Where(r => r.Success))
            {
                if (result.InstallationState.ProviderId != null)
                {
                    telResult.TryGetValue(result.InstallationState.ProviderId, out double count);
                    telResult[result.InstallationState.ProviderId] = count + 1;
                }
            }
            telResult.Add("time", elapsedTime.TotalMilliseconds);
            Telemetry.TrackUserTask("restore", TelemetryResult.None, telResult.Select(i => new KeyValuePair<string, object>(i.Key, i.Value)).ToArray());
        }

        private static void AddErrorsToErrorList(string projectName, string configFile, IEnumerable<ILibraryInstallationResult> results)
        {
            var errorList = new ErrorList(projectName, configFile);
            errorList.HandleErrors(results);
        }

        public static async Task UninstallAsync(string configFilePath, string libraryId, CancellationToken cancellationToken)
        {
            Logger.LogEventsHeader(OperationType.Uninstall);

            Stopwatch sw = new Stopwatch();
            var dependencies = Dependencies.FromConfigFile(configFilePath);
            Manifest manifest = await Manifest.FromFileAsync(configFilePath, dependencies, cancellationToken).ConfigureAwait(false);
            IHostInteraction hostInteraction = dependencies.GetHostInteractions();

            sw.Start();
            ILibraryInstallationResult result = await manifest.UninstallAsync(libraryId, async (filesPaths) => await hostInteraction.DeleteFilesAsync(filesPaths, cancellationToken), cancellationToken);
            sw.Stop();

            Logger.LogEventsSummary(new List<ILibraryInstallationResult> { result }, OperationType.Uninstall, sw.Elapsed);
            Telemetry.TrackUserTask("libraryuninstall");
        }

        public static async Task CleanAsync(ProjectItem configProjectItem, CancellationToken cancellationToken)
        {
            Logger.LogEventsHeader(OperationType.Clean);

            Stopwatch sw = new Stopwatch();
            string configFileName = configProjectItem.FileNames[1];
            var dependencies = Dependencies.FromConfigFile(configFileName);
            Manifest manifest = await Manifest.FromFileAsync(configFileName, dependencies, CancellationToken.None).ConfigureAwait(false);
            IHostInteraction hostInteraction = dependencies.GetHostInteractions();

            if (manifest != null)
            {
                sw.Start();
                IEnumerable<ILibraryInstallationResult> results = await manifest.CleanAsync(async (filesPaths) => await hostInteraction.DeleteFilesAsync(filesPaths, cancellationToken), cancellationToken);
                sw.Stop();

                Logger.LogEventsSummary(results, OperationType.Clean, sw.Elapsed);
            }
        }

        private static async Task<IEnumerable<ILibraryInstallationResult>> RestoreLibrariesAsync(Manifest manifest, CancellationToken cancellationToken)
        {
            return await manifest.RestoreAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task AddFilesToProjectAsync(string configFilePath, Project project, IEnumerable<ILibraryInstallationResult> results, CancellationToken cancellationToken)
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

            if (project != null)
            {
                var logAction = new Action<string, LogLevel>((message, level) => { Logger.LogEvent(message, level); });
                await VsHelpers.AddFilesToProjectAsync(project, files, logAction, cancellationToken);
            }
        }
    }
}
