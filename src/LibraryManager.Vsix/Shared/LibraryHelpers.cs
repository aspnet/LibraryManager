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
            Stopwatch sw = new Stopwatch();

            Logger.LogEvent(LibraryManager.Resources.Text.RestoringLibraries, LogLevel.Task);
            List<ILibraryInstallationResult> totalResults = new List<ILibraryInstallationResult>();

            sw.Start();

            foreach (KeyValuePair<string, Manifest> manifest in manifests)
            {
                Project project = VsHelpers.DTE.Solution?.FindProjectItem(manifest.Key)?.ContainingProject;
                Logger.LogEvent(string.Format("Restoring packages for {0}...", project.FullName), LogLevel.Operation);

                IEnumerable<ILibraryInstallationResult> results = await RestoreLibrariesAsync(manifest.Value, cancellationToken).ConfigureAwait(false);

                AddFilesToProject(manifest.Key, project, results);
                AddErrorsToErrorList(project?.Name, manifest.Key, results);

                totalResults.AddRange(results);
            }

            sw.Stop();

            Logger.LogEvent("Restoring libraries completed", LogLevel.Status);

            PostRestoreTelemetryData(totalResults, sw.Elapsed);
            LogEventsSummary(totalResults, sw.Elapsed);

        }

        private static void LogEventsSummary(List<ILibraryInstallationResult> totalResults, TimeSpan elapsedTime)
        {
            int totalResultsCounts = totalResults.Count();
            IEnumerable<ILibraryInstallationResult> successfulRestores = totalResults.Where(r => r.Success);
            IEnumerable<ILibraryInstallationResult> failedRestores = totalResults.Where(r => !r.Success);
            IEnumerable<ILibraryInstallationResult> cancelledRestores = totalResults.Where(r => r.Cancelled);
            IEnumerable<ILibraryInstallationResult> upToDateRestores = totalResults.Where(r => r.UpToDate);

            bool allSuccess = successfulRestores.Count() == totalResultsCounts;
            bool allFailed = failedRestores.Count() == totalResultsCounts;
            bool allCancelled = cancelledRestores.Count() == totalResultsCounts;
            bool allUpToDate = upToDateRestores.Count() == totalResultsCounts;
            bool partialSuccess = successfulRestores.Count() < totalResultsCounts;

            if (allUpToDate)
            {
                Logger.LogEvent(Resources.Text.LibraryRestoredNoChange + Environment.NewLine, LogLevel.Operation);
            }
            else if (allSuccess)
            {
                string successText = string.Format(LibraryManager.Resources.Text.LibrariesRestored, totalResultsCounts, Math.Round(elapsedTime.TotalSeconds, 2));
                Logger.LogEvent(successText + Environment.NewLine, LogLevel.Operation);
            }
            else if (allCancelled)
            {
                string canceledText = string.Format(LibraryManager.Resources.Text.LibraryRestorationCancelled, totalResultsCounts, Math.Round(elapsedTime.TotalSeconds, 2));
                Logger.LogEvent(canceledText + Environment.NewLine, LogLevel.Operation);
            }
            else if (allFailed)
            {
                string failedText = string.Format(LibraryManager.Resources.Text.LibraryRestorationFailed, totalResultsCounts, Math.Round(elapsedTime.TotalSeconds, 2));
                Logger.LogEvent(failedText + Environment.NewLine, LogLevel.Operation);
            }
            else
            {
                var summarySuccessText = string.Format("{0} libraries restored successfuly", successfulRestores.Count());
                Logger.LogEvent(summarySuccessText + Environment.NewLine, LogLevel.Operation);
                foreach (var result in successfulRestores)
                {
                    var successText = string.Format("Successfuly restored library: {0}", result.InstallationState.LibraryId);
                    Logger.LogEvent(successText, LogLevel.Operation);
                }

                if (failedRestores.Any())
                {
                    var summaryErrorText = string.Format("{0} libraries failed to restore", failedRestores.Count());
                    Logger.LogEvent(Environment.NewLine + summaryErrorText + Environment.NewLine, LogLevel.Operation);
                    foreach (var result in failedRestores)
                    {
                        var errorText = string.Format("Failed to restore library: {0}", result.InstallationState.LibraryId);
                        Logger.LogEvent(errorText, LogLevel.Operation);
                    }
                }

                if (cancelledRestores.Any())
                {
                    var summaryCancellationText = string.Format("{0} libraries had restore cancelled", cancelledRestores.Count());
                    Logger.LogEvent(Environment.NewLine + summaryCancellationText + Environment.NewLine, LogLevel.Operation);
                    foreach (var result in cancelledRestores)
                    {
                        var cancellationText = string.Format("Cancelled restore of library: {0}", result.InstallationState.LibraryId);
                        Logger.LogEvent(cancellationText, LogLevel.Operation);
                    }
                }
            }

            Logger.LogEvent(string.Format("Time Elapsed: {0}", elapsedTime), LogLevel.Operation);
            Logger.LogEvent("========== Finished ==========" + Environment.NewLine, LogLevel.Operation);
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
            var dependencies = Dependencies.FromConfigFile(configFilePath);
            Manifest manifest = await Manifest.FromFileAsync(configFilePath, dependencies, cancellationToken).ConfigureAwait(false);
            var hostInteraction = dependencies.GetHostInteractions() as HostInteraction;
            await manifest.UninstallAsync(libraryId, async (file) => await hostInteraction.DeleteFilesAsync(file, cancellationToken), cancellationToken);

            Telemetry.TrackUserTask("libraryuninstall");
        }

        public static async Task CleanAsync(ProjectItem configProjectItem, CancellationToken cancellationToken)
        {
            Stopwatch sw = new Stopwatch();
            Logger.LogEvent(Resources.Text.CleanLibrariesStarted, LogLevel.Task);

            string configFileName = configProjectItem.FileNames[1];
            var dependencies = Dependencies.FromConfigFile(configFileName);
            Manifest manifest = await Manifest.FromFileAsync(configFileName, dependencies, CancellationToken.None).ConfigureAwait(false);
            var hostInteraction = dependencies.GetHostInteractions() as HostInteraction;

            sw.Start();

            IEnumerable<ILibraryInstallationResult> results = await manifest?.CleanAsync(async (file) => await hostInteraction.DeleteFilesAsync(file, cancellationToken), cancellationToken);

            sw.Stop();

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

            Logger.LogEvent(string.Format("Time Elapsed: {0}", sw.Elapsed), LogLevel.Operation);
            Logger.LogEvent("========== Finished ==========" + Environment.NewLine, LogLevel.Operation);
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
