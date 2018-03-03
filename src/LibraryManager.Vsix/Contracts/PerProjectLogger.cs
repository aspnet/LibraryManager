using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix.Contracts
{
    internal class PerProjectLogger : ILogger
    {
        private string _projectName;

        public PerProjectLogger(string projectName)
        {
            _projectName = projectName;
        }

        public void Log(string message, LogLevel level)
        {
            Logger.Log($"{message} ({_projectName})", level);
        }
    }
}
