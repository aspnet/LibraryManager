// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using LibraryInstaller.Contracts;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace LibraryInstaller.Build
{
    public class RestoreTask : Task
    {
        /// <summary>
        /// The file path of the compilerconfig.json file
        /// </summary>
        public string FileName { get; set; }

        public override bool Execute()
        {
            var configFilePath = new FileInfo(FileName);

            if (!configFilePath.Exists)
            {
                Log.LogWarning(configFilePath.Name + " does not exist");
                return true;
            }

            var sw = new Stopwatch();
            sw.Start();

            CancellationToken token = CancellationToken.None;

            Log.LogMessage(MessageImportance.High, Environment.NewLine + Resources.Text.RestoringLibraries);

            var dependencies = Dependencies.FromTask(this);
            Manifest manifest = Manifest.FromFileAsync(configFilePath.FullName, dependencies, token).Result;

            IEnumerable<ILibraryInstallationResult> result = manifest.RestoreAsync(token).Result;

            sw.Stop();

            int fileCount = result.Sum(r => r.InstallationState.Files.Count);
            bool hasErrors = result.Any(r => !r.Success);

            if (fileCount > 0)
            {
                string text = hasErrors ?
                    Resources.Text.RestoreHasErrors :
                    string.Format(Resources.Text.LibrariesRestored, fileCount, Math.Round(sw.Elapsed.TotalSeconds, 2));

                Log.LogMessage(MessageImportance.High, Environment.NewLine + text + Environment.NewLine);
            }
            else
            {
                Log.LogMessage(MessageImportance.High, Environment.NewLine + "Restore completed. Files already up-to-date" + Environment.NewLine);
            }

            return true;
        }
    }
}
