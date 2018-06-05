using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Web.LibraryManager.Contracts;

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


        public void LogOperationSummary(IEnumerable<ILibraryInstallationResult> results, OperationType operation, TimeSpan elapsedTime)
        {
            int totalResultsCounts = results.Count();
            IEnumerable<ILibraryInstallationResult> successfulRestores = results.Where(r => r.Success);
            IEnumerable<ILibraryInstallationResult> failedRestores = results.Where(r => !r.Success);
            IEnumerable<ILibraryInstallationResult> cancelledRestores = results.Where(r => r.Cancelled);
            IEnumerable<ILibraryInstallationResult> upToDateRestores = results.Where(r => r.UpToDate);

            bool allSuccess = successfulRestores.Count() == totalResultsCounts;
            bool allFailed = failedRestores.Count() == totalResultsCounts;
            bool allCancelled = cancelledRestores.Count() == totalResultsCounts;
            bool allUpToDate = upToDateRestores.Count() == totalResultsCounts;
            bool partialSuccess = successfulRestores.Count() < totalResultsCounts;

            Logger.LogEvent(LibraryManager.Resources.Text.Restore_OperationCompleted, LogLevel.Status);

            if (allUpToDate)
            {
                Logger.LogEvent(LibraryManager.Resources.Text.Restore_LibrariesUptodate + Environment.NewLine, LogLevel.Operation);
            }
            else if (allSuccess)
            {
                string successText = string.Format(LibraryManager.Resources.Text.Restore_NumberOfLibrariesSucceeded, totalResultsCounts, Math.Round(elapsedTime.TotalSeconds, 2));
                Logger.LogEvent(successText + Environment.NewLine, LogLevel.Operation);
            }
            else if (allCancelled)
            {
                string canceledText = string.Format(LibraryManager.Resources.Text.Restore_NumberOfLibrariesCancelled, totalResultsCounts, Math.Round(elapsedTime.TotalSeconds, 2));
                Logger.LogEvent(canceledText + Environment.NewLine, LogLevel.Operation);
            }
            else if (allFailed)
            {
                string failedText = string.Format(LibraryManager.Resources.Text.Restore_NumberOfLibrariesFailed, totalResultsCounts, Math.Round(elapsedTime.TotalSeconds, 2));
                Logger.LogEvent(failedText + Environment.NewLine, LogLevel.Operation);
            }

            Logger.LogEvent(string.Format(LibraryManager.Resources.Text.TimeElapsed, elapsedTime), LogLevel.Operation);
            Logger.LogEvent(LibraryManager.Resources.Text.SummaryEndLine + Environment.NewLine, LogLevel.Operation);
        }
    }
}
