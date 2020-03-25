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
using Microsoft.Web.LibraryManager.Build.Contracts;
using Logger = Microsoft.Web.LibraryManager.Build.Contracts.Logger;

namespace Microsoft.Web.LibraryManager.Build
{
    public class RestoreTask
#if NET472
        : AppDomainIsolatedTask
#else
        : Task
#endif
    {
        [Required]
        public string FileName { get; set; }

        [Required]
        public string ProjectDirectory { get; set; }

#pragma warning disable CA1819 // Properties should not return arrays
        [Required]
        public ITaskItem[] ProviderAssemblies { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays

#pragma warning disable CA1819 // Properties should not return arrays
        [Output]
        public ITaskItem[] FilesWritten { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays

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

            Log.LogMessage(MessageImportance.High, Environment.NewLine + Resources.Text.Restore_OperationStarted);

            var dependencies = Dependencies.FromTask(ProjectDirectory, ProviderAssemblies.Select(pa => new FileInfo(pa.ItemSpec).FullName));
            Manifest manifest = Manifest.FromFileAsync(configFilePath.FullName, dependencies, token).Result;
            var logger = dependencies.GetHostInteractions().Logger as Logger;

            if (manifest == null)
            {
                sw.Stop();
                LogErrors(new[] { PredefinedErrors.ManifestMalformed() });
                FlushLogger(logger);

                return false;
            }

            IEnumerable<ILibraryOperationResult> validationResults = manifest.GetValidationResultsAsync(token).Result;
            if (!validationResults.All(r => r.Success))
            {
                sw.Stop();
                LogErrors(validationResults.SelectMany(r =>r.Errors));

                return false;
            }

            IEnumerable<ILibraryOperationResult> results = manifest.RestoreAsync(token).Result;

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
                Log.LogMessage(message);
            }

            foreach (string error in logger.Errors)
            {
                Log.LogError(error);
            }
        }

        private void LogResults(Stopwatch sw, IEnumerable<ILibraryOperationResult> results)
        {
            bool hasErrors = results.Any(r => !r.Success);

            if (hasErrors)
            {
                LogErrors(results.SelectMany(r => r.Errors));
            }
            else
            {
                int fileCount = results.Where(r => r.Success).Sum(r => r.InstallationState.Files.Count);
                if (fileCount > 0)
                {
                    string text = string.Format(Resources.Text.Restore_NumberOfLibrariesSucceeded, results.Count(), Math.Round(sw.Elapsed.TotalSeconds, 2));
                    Log.LogMessage(MessageImportance.High, Environment.NewLine + text + Environment.NewLine);
                }
                else
                {
                    Log.LogMessage(MessageImportance.High, Environment.NewLine + "Restore completed. Files already up-to-date" + Environment.NewLine);
                }
            }
        }

        private void LogErrors(IEnumerable<IError> errors)
        {
            foreach (IError error in errors)
            {
                Log.LogError(null, error.Code, null, FileName, 0, 0, 0, 0, error.Message);
            }

            string text = Resources.Text.Restore_OperationHasErrors;
            Log.LogMessage(MessageImportance.High, Environment.NewLine + text + Environment.NewLine);
        }

        private void PopulateFilesWritten(IEnumerable<ILibraryOperationResult> results, IHostInteraction hostInteraction)
        {
            IEnumerable<ILibraryInstallationState> states = results.Where(r => r.Success).Select(r => r.InstallationState);
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
