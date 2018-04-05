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
            Logger.LogEvent(LibraryManager.Resources.Text.RestoringLibraries, LogLevel.Status);

            var sw = new Stopwatch();
            sw.Start();
            int resultCount = 0;
            bool hasErrors = false;
            var telResult = new Dictionary<string, double>();

            foreach (KeyValuePair<string, Manifest> manifest in manifests)
            {
                IEnumerable<ILibraryInstallationResult> results = await RestoreLibrariesAsync(manifest.Value, cancellationToken).ConfigureAwait(false);

                Project project = VsHelpers.DTE.Solution?.FindProjectItem(manifest.Key)?.ContainingProject;
                AddFilesToProject(manifest.Key, project, results);

                var errorList = new ErrorList(project?.Name, manifest.Key);
                hasErrors |= errorList.HandleErrors(results);

                resultCount += results.Count();

                foreach (ILibraryInstallationResult result in results.Where(r => r.Success))
                {
                    if (result.InstallationState.ProviderId != null)
                    {
                        telResult.TryGetValue(result.InstallationState.ProviderId, out double count);
                        telResult[result.InstallationState.ProviderId] = count + 1;
                    }
                }
            }

            sw.Stop();

            telResult.Add("time", sw.Elapsed.TotalMilliseconds);
            Telemetry.TrackUserTask("restore", TelemetryResult.None, telResult.Select(i => new KeyValuePair<string, object>(i.Key, i.Value)).ToArray());

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

            IEnumerable<ILibraryInstallationResult> results = manifest?.Clean((file) => hostInteraction.DeleteFiles(file));

            if (results != null && results.All(r => r.Success))
            {
                Logger.LogEvent(Resources.Text.CleanLibrariesSucceeded + Environment.NewLine, LogLevel.Task);
                Telemetry.TrackUserTask("clean", TelemetryResult.Success, new KeyValuePair<string, object>("librariesdeleted", results.Count()));
            }
            else
            {
                Logger.LogEvent(Resources.Text.CleanLibrariesFailed + Environment.NewLine, LogLevel.Task);
                Telemetry.TrackUserTask("clean", TelemetryResult.Failure, new KeyValuePair<string, object>("librariesfailedtodelete", results.Where(r => !r.Success).Count()));
            }
        }

        private static async Task<IEnumerable<ILibraryInstallationResult>> RestoreLibrariesAsync(Manifest manifest, CancellationToken cancellationToken)
        {
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
