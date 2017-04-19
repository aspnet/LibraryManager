// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryInstaller.Contracts;
using System.Collections.Generic;
using System.Linq;

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
            foreach (ILibraryInstallationResult result in results)
            {
                if (!result.Success)
                {
                    IEnumerable<DisplayError> displayErrors = result.Errors.Select(error => new DisplayError(error));
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
