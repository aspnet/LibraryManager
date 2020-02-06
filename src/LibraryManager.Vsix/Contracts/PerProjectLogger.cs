using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Vsix.Shared;

namespace Microsoft.Web.LibraryManager.Vsix.Contracts
{
    internal class PerProjectLogger : ILogger
    {
        private string _configFileName;
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
