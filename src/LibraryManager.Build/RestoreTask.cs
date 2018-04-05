// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Microsoft.Web.LibraryManager.Build
{
    internal class RestoreTask : Task
    {
        [Required]
        public string FileName { get; set; }

        [Required]
        public string ProjectDirectory { get; set; }

        [Required]
        public ITaskItem[] ProviderAssemblies { get; set; }

        [Output]
        public ITaskItem[] FilesWritten { get; set; }

        public override bool Execute()
        {
            Logger.Instance.Clear();
            var configFilePath = new FileInfo(Path.Combine(ProjectDirectory, FileName));

            if (!configFilePath.Exists)
            {
                Log.LogWarning(configFilePath.Name + " does not exist");
                return true;
            }

            var sw = new Stopwatch();
            sw.Start();

            CancellationToken token = CancellationToken.None;

            Log.LogMessage(MessageImportance.High, Environment.NewLine + Resources.Text.RestoringLibraries);

            var dependencies = Dependencies.FromTask(ProjectDirectory, ProviderAssemblies.Select(pa => new FileInfo(pa.ItemSpec).FullName));
            Manifest manifest = Manifest.FromFileAsync(configFilePath.FullName, dependencies, token).Result;
            var logger = dependencies.GetHostInteractions().Logger as Logger;

            if (manifest == null)
            {
                logger.Log(PredefinedErrors.ManifestMalformed().Message, LogLevel.Error);
                FlushLogger(logger);
                return false;
            }

            IEnumerable<ILibraryInstallationResult> results = manifest.RestoreAsync(token).Result;

            sw.Stop();

            FlushLogger(logger);
            PopulateFilesWritten(results, dependencies.GetHostInteractions());
            LogResults(sw, results);

            return !Log.HasLoggedErrors;
        }

        // This is done to fix the issue with async/await in a synchronous Execute() method
        private void FlushLogger(Logger logger)
        {
            foreach (string message in logger.Messages)
            {
                Log.LogMessage(MessageImportance.High, message);
            }

            foreach (string error in logger.Errors)
            {
                Log.LogError(error);
            }
        }

        private void LogResults(Stopwatch sw, IEnumerable<ILibraryInstallationResult> results)
        {
            int fileCount = results.Sum(r => r.InstallationState.Files.Count);
            bool hasErrors = results.Any(r => !r.Success);

            foreach (IError error in results.SelectMany(r => r.Errors))
            {
                Log.LogWarning(null, error.Code, null, FileName, 0, 0, 0, 0, error.Message);
            }

            if (fileCount > 0)
            {
                string text = hasErrors ?
                    Resources.Text.RestoreHasErrors :
                    string.Format(Resources.Text.LibrariesRestored, results.Count(), Math.Round(sw.Elapsed.TotalSeconds, 2));

                Log.LogMessage(MessageImportance.High, Environment.NewLine + text + Environment.NewLine);
            }
            else
            {
                Log.LogMessage(MessageImportance.High, Environment.NewLine + "Restore completed. Files already up-to-date" + Environment.NewLine);
            }
        }

        private void PopulateFilesWritten(IEnumerable<ILibraryInstallationResult> results, IHostInteraction hostInteraction)
        {
            IEnumerable<ILibraryInstallationState> states = results.Select(r => r.InstallationState);
            var list = new List<ITaskItem>();

            foreach (ILibraryInstallationState state in states)
            {
                foreach (string file in state.Files)
                {
                    string absolutePath = Path.Combine(hostInteraction.WorkingDirectory, state.DestinationPath, file);
                    var absolute = new FileInfo(absolutePath);

                    if (absolute.Exists)
                    {
                        string relative = absolute.FullName.Replace(ProjectDirectory, string.Empty).TrimStart('/', '\\');
                        list.Add(new TaskItem(relative));
                    }
                }
            }

            FilesWritten = list.ToArray();
        }
    }
}
