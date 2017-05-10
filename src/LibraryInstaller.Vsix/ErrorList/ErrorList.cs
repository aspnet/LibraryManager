// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryInstaller.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Microsoft.Web.LibraryInstaller.Vsix
{
    public class ErrorList
    {
        public ErrorList(string projectName, string configFileName)
        {
            ProjectName = projectName;
            ConfigFileName = configFileName;
            Errors = new List<DisplayError>();
        }

        public string ProjectName { get; set; }
        public string ConfigFileName { get; set; }
        public List<DisplayError> Errors { get; }

        public bool HandleErrors(IEnumerable<ILibraryInstallationResult> results)
        {
            IEnumerable<string> json = File.Exists(ConfigFileName) ? File.ReadLines(ConfigFileName) : new string[0];

            foreach (ILibraryInstallationResult result in results)
            {
                if (!result.Success)
                {
                    DisplayError[] displayErrors = result.Errors.Select(error => new DisplayError(error)).ToArray();
                    AddLineAndColumn(json, result.InstallationState, displayErrors);

                    Errors.AddRange(displayErrors);

                    foreach (IError error in result.Errors)
                    {
                        Logger.LogEvent(error.Message, LogLevel.Operation);
                        Telemetry.TrackOperation("error", new KeyValuePair<string, object>("code", error.Code));
                    }
                }
            }

            PushToErrorList();
            return Errors.Count > 0;
        }

        private static void AddLineAndColumn(IEnumerable<string> lines, ILibraryInstallationState state, DisplayError[] errors)
        {
            if(string.IsNullOrEmpty(state?.LibraryId))
            {
                return;
            }

            foreach (DisplayError error in errors)
            {
                int index = 0;

                for (int i = 0; i < lines.Count(); i++)
                {
                    string line = lines.ElementAt(i);

                    if (line.Trim() == "{")
                        index = i;

                    if (line.Contains(state.LibraryId))
                    {
                        error.Line = index > 0 ? index : i;
                        break;
                    }
                }
            }
        }

        private void PushToErrorList()
        {
            TableDataSource.Instance.CleanErrors(ConfigFileName);

            if (Errors.Count > 0)
            {
                TableDataSource.Instance.AddErrors(Errors, ProjectName, ConfigFileName);
            }
        }
    }
}
