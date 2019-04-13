// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix.Contracts
{
    internal class PerProjectLogger : ILogger
    {
        private readonly string _configFileName;
        private string _projectName;

        private string ProjectName
        {
            get
            {
                if (string.IsNullOrEmpty(_projectName))
                {
                    string projectName = VsHelpers.GetDTEProjectFromConfig(_configFileName)?.Name;
                    _projectName = string.IsNullOrEmpty(projectName) ? string.Empty : $" ({projectName})";
                }

                return _projectName;
            }
        }

        public PerProjectLogger(string configFileName)
        {
            _configFileName = configFileName;
        }

        public void Log(string message, LogLevel level)
        {
            Logger.LogEvent($"{message}{ProjectName}", level);
        }

    }
}
