// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.LibraryNaming;

namespace Microsoft.Web.LibraryManager.Vsix.ErrorList
{
    internal class ErrorListPropagator
    {
        public ErrorListPropagator(string projectName, string configFileName)
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
            string[] jsonLines = File.Exists(ConfigFileName) ? File.ReadLines(ConfigFileName).ToArray() : Array.Empty<string>();

            foreach (ILibraryOperationResult result in results)
            {
                if (!result.Success)
                {
                    DisplayError[] displayErrors = result.Errors.Select(error => new DisplayError(error)).ToArray();
                    AddLineAndColumn(jsonLines, result.InstallationState, displayErrors);

                    Errors.AddRange(displayErrors);
                }
            }

            PushToErrorList();
            return Errors.Count > 0;
        }

        private static void AddLineAndColumn(string[] lines, ILibraryInstallationState state, DisplayError[] errors)
        {
            string libraryId = LibraryIdToNameAndVersionConverter.Instance.GetLibraryId(state?.Name, state?.Version, state?.ProviderId);

            if(string.IsNullOrEmpty(libraryId))
            {
                return;
            }

            foreach (DisplayError error in errors)
            {
                int index = 0;

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];

                    if (line.Trim() == "{")
                        index = i;

                    if (line.Contains(libraryId))
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
