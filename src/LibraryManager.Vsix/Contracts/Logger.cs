// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal static class Logger
    {
        private static Guid _outputPaneGuid = new Guid("cce35aef-ace6-4371-b1e1-8efa3cdc8324");
        private static IVsOutputWindowPane _pane;
        private static readonly IVsOutputWindow _output = VsHelpers.GetService<SVsOutputWindow, IVsOutputWindow>();
        private static readonly IVsActivityLog _activityLog = VsHelpers.GetService<SVsActivityLog, IVsActivityLog>();
        private static readonly IVsStatusbar _statusbar = VsHelpers.GetService<SVsStatusbar, IVsStatusbar>();


        public static void LogEvent(string message, LogLevel level)
        {
            try
            {
                switch (level)
                {
                    case LogLevel.Operation:
                        LogToOutputWindow(message);
                        break;
                    case LogLevel.Error:
                        LogToActivityLog(message, __ACTIVITYLOG_ENTRYTYPE.ALE_ERROR);
                        break;
                    case LogLevel.Task:
                        LogToStatusBar(message);
                        LogToOutputWindow(message);
                        break;
                    case LogLevel.Status:
                        LogToStatusBar(message);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex);
            }
        }

        public static void ClearOutputWindow()
        {
            _pane?.Clear();
        }

        private static void LogToActivityLog(string message, __ACTIVITYLOG_ENTRYTYPE type)
        {
            _activityLog.LogEntry((uint)type, Vsix.Name, message);
        }

        public static void LogToStatusBar(string message)
        {
            _statusbar.FreezeOutput(0);
            _statusbar.SetText(message);
            _statusbar.FreezeOutput(1);
        }

        private static void LogToOutputWindow(object message)
        {
            if (EnsurePane())
            {
                _pane.OutputString(message + Environment.NewLine);
            }
        }

        private static bool EnsurePane()
        {
            if (_pane == null)
            {
                if (_output != null)
                {
                    if (ErrorHandler.Failed(_output.GetPane(ref _outputPaneGuid, out _pane)) &&
                        ErrorHandler.Succeeded(_output.CreatePane(ref _outputPaneGuid, Vsix.Name, 0, 0)))
                    {
                        if (ErrorHandler.Succeeded(_output.GetPane(ref _outputPaneGuid, out _pane)))
                        {
                            _pane.Activate();
                        }
                    }
                }
            }

            return _pane != null;
        }

        internal static void LogEventsHeader(OperationType operationType)
        {
            LogEvent(GetOperationHeaderString(operationType), LogLevel.Task);
        }

        internal static void LogEventsSummary(IEnumerable<ILibraryInstallationResult> totalResults, OperationType operationType, TimeSpan elapsedTime )
        {
            LogEvent(GetOperationHeaderString(operationType), LogLevel.Status);
            LogOperationSummary(totalResults, operationType, elapsedTime);
            LogEvent(string.Format(LibraryManager.Resources.Text.TimeElapsed, elapsedTime), LogLevel.Operation);
            LogEvent(LibraryManager.Resources.Text.SummaryEndLine + Environment.NewLine, LogLevel.Operation);
        }

        private static void LogOperationSummary(IEnumerable<ILibraryInstallationResult> totalResults, OperationType operation, TimeSpan elapsedTime)
        {
            int totalResultsCounts = totalResults.Count();
            IEnumerable<ILibraryInstallationResult> successfulRestores = totalResults.Where(r => r.Success);
            IEnumerable<ILibraryInstallationResult> failedRestores = totalResults.Where(r => !r.Success);
            IEnumerable<ILibraryInstallationResult> cancelledRestores = totalResults.Where(r => r.Cancelled);
            IEnumerable<ILibraryInstallationResult> upToDateRestores = totalResults.Where(r => r.UpToDate);

            bool allSuccess = successfulRestores.Count() == totalResultsCounts;
            bool allFailed = failedRestores.Count() == totalResultsCounts;
            bool allCancelled = cancelledRestores.Count() == totalResultsCounts;
            bool allUpToDate = upToDateRestores.Count() == totalResultsCounts;
            bool partialSuccess = successfulRestores.Count() < totalResultsCounts;

            LogEvent(GetSummaryHeaderString(operation) + Environment.NewLine, LogLevel.Task); 
            if (allUpToDate)
            {
                LogEvent(LibraryManager.Resources.Text.Restore_LibrariesUptodate + Environment.NewLine, LogLevel.Operation);
            }
            else if (allSuccess)
            {
                string successText = operation == OperationType.Clean ?
                                     string.Format(LibraryManager.Resources.Text.Clean_NumberOfLibrariesSucceeded, totalResultsCounts, Math.Round(elapsedTime.TotalSeconds, 2)) :
                                     string.Format(LibraryManager.Resources.Text.Restore_NumberOfLibrariesSucceeded, totalResultsCounts, Math.Round(elapsedTime.TotalSeconds, 2));

                LogEvent(successText + Environment.NewLine, LogLevel.Operation);
            }
            else if (allCancelled)
            {
                string canceledText = operation == OperationType.Clean ?
                                    string.Format(LibraryManager.Resources.Text.Clean_OperationCancelled, totalResultsCounts):
                                    string.Format(LibraryManager.Resources.Text.Restore_OperationCancelled, totalResultsCounts);

                LogEvent(canceledText + Environment.NewLine, LogLevel.Operation);
            }
            else if (allFailed)
            {
                string failedText = operation == OperationType.Clean ?
                                    string.Format(LibraryManager.Resources.Text.Clean_NumberOfLibrariesFailed, totalResultsCounts):
                                    string.Format(LibraryManager.Resources.Text.Restore_NumberOfLibrariesFailed, totalResultsCounts);

                LogEvent(failedText + Environment.NewLine, LogLevel.Operation);
            }
            else
            {
                var summarySuccessText = operation == OperationType.Clean ?
                                                      string.Format(LibraryManager.Resources.Text.Clean_NumberOfLibrariesSucceeded, successfulRestores.Count()):
                                                      string.Format(LibraryManager.Resources.Text.Restore_NumberOfLibrariesSucceeded, successfulRestores.Count());
                LogEvent(summarySuccessText + Environment.NewLine, LogLevel.Operation);

                foreach (var result in successfulRestores)
                {
                    var successText = string.Format(LibraryManager.Resources.Text.Restore_LibraryRestoreSucceeded, result.InstallationState.LibraryId);
                    LogEvent(successText, LogLevel.Operation);
                }

                if (failedRestores.Any())
                {
                    var summaryErrorText = operation == OperationType.Clean ?
                                           string.Format(LibraryManager.Resources.Text.Clean_NumberOfLibrariesFailed, failedRestores.Count()):
                                           string.Format(LibraryManager.Resources.Text.Restore_NumberOfLibrariesFailed, failedRestores.Count());
                    LogEvent(Environment.NewLine + summaryErrorText + Environment.NewLine, LogLevel.Operation);

                    foreach (var result in failedRestores)
                    {
                        var errorText = operation == OperationType.Clean ?
                                        string.Format(LibraryManager.Resources.Text.Clean_NumberOfLibrariesFailed, result.InstallationState.LibraryId):
                                        string.Format(LibraryManager.Resources.Text.Restore_NumberOfLibrariesFailed, result.InstallationState.LibraryId);
                        LogEvent(errorText, LogLevel.Operation);
                    }
                }

                if (cancelledRestores.Any())
                {
                    var summaryCancellationText = operation == OperationType.Clean ?
                                                    string.Format(LibraryManager.Resources.Text.Clean_NumberOfLibrariesCancelled, cancelledRestores.Count()):
                                                    string.Format(LibraryManager.Resources.Text.Restore_NumberOfLibrariesCancelled, cancelledRestores.Count());
                    LogEvent(Environment.NewLine + summaryCancellationText + Environment.NewLine, LogLevel.Operation);

                    foreach (var result in cancelledRestores)
                    {
                        var cancellationText = operation == OperationType.Clean ?
                            string.Format(LibraryManager.Resources.Text.Clean_OperationCancelled, result.InstallationState.LibraryId):
                            string.Format(LibraryManager.Resources.Text.Restore_OperationCancelled, result.InstallationState.LibraryId);
                        LogEvent(cancellationText, LogLevel.Operation);
                    }
                }
            }
        }

        private static string GetOperationHeaderString(OperationType operationType)
        {
            switch (operationType)
            {
                case OperationType.Restore:
                {
                    return LibraryManager.Resources.Text.Restore_OperationStarted;
                }
                case OperationType.Clean:
                {
                    return LibraryManager.Resources.Text.Clean_OperationStarted;
                }
            }

            return string.Empty;
        }

        private static string GetSummaryHeaderString(OperationType operationType)
        {
            switch (operationType)
            {
                case OperationType.Restore:
                {
                    return LibraryManager.Resources.Text.Restore_OperationCompleted;
                }
                case OperationType.Clean:
                {
                    return LibraryManager.Resources.Text.Clean_OperationCompleted;
                }
            }

            return string.Empty;
        }
    }
}
