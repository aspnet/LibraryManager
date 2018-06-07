// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal class ErrorList
    {
        public ErrorList(string projectName, string configFileName)
        {
            ProjectName = projectName ?? "";
            ConfigFileName = configFileName;
            Errors = new List<DisplayError>();
        }

        public string ProjectName { get; set; }
        public string ConfigFileName { get; set; }
        public List<DisplayError> Errors { get; }

        public bool HandleErrors(IEnumerable<ILibraryOperationResult> results)
        {
            IEnumerable<string> json = File.Exists(ConfigFileName) ? File.ReadLines(ConfigFileName) : new string[0];

            foreach (ILibraryOperationResult result in results)
            {
                if (!result.Success)
                {
                    DisplayError[] displayErrors = result.Errors.Select(error => new DisplayError(error)).ToArray();
                    AddLineAndColumn(json, result.InstallationState, displayErrors);

                    Errors.AddRange(displayErrors);

                    foreach (IError error in result.Errors)
                    {
                        Logger.LogEvent(error.Message, LogLevel.Operation);
                        Telemetry.TrackOperation("error", TelemetryResult.Failure, new KeyValuePair<string, object>("code", error.Code));
                    }
                }
            }

            PushToErrorList();
            return Errors.Count > 0;
        }

        public void HandleError(IError error)
        {
            Errors.Add(new DisplayError(error));

            Logger.LogEvent(error.Message, LogLevel.Operation);
            Telemetry.TrackOperation("error", TelemetryResult.Failure, new KeyValuePair<string, object>("code", error.Code));

            PushToErrorList();
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

        public void ClearErrors()
        {
            TableDataSource.Instance.CleanErrors(ConfigFileName);
        }
    }
}
