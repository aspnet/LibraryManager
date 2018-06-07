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
                        ErrorHandler.Succeeded(_output.CreatePane(ref _outputPaneGuid, Resources.Text.OutputWindowTitle, 0, 0)))
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

        internal static void LogEventsHeader(OperationType operationType, string libraryId)
        {
            LogEvent(GetOperationHeaderString(operationType, libraryId), LogLevel.Task);
        }

        internal static void LogEventsSummary(IEnumerable<ILibraryOperationResult> totalResults, OperationType operationType, TimeSpan elapsedTime )
        {
            LogEvent(GetSummaryHeaderString(operationType, null), LogLevel.Task);
            LogOperationSummary(totalResults, operationType, elapsedTime);
            LogEvent(string.Format(LibraryManager.Resources.Text.TimeElapsed, elapsedTime), LogLevel.Operation);
            LogEvent(LibraryManager.Resources.Text.SummaryEndLine + Environment.NewLine, LogLevel.Operation);
        }

        private static void LogOperationSummary(IEnumerable<ILibraryOperationResult> totalResults, OperationType operation, TimeSpan elapsedTime)
        {
            int totalResultsCounts = totalResults.Count();
            IEnumerable<ILibraryOperationResult> successfulRestores = totalResults.Where(r => r.Success && !r.UpToDate);
            IEnumerable<ILibraryOperationResult> failedRestores = totalResults.Where(r => r.Errors.Any());
            IEnumerable<ILibraryOperationResult> cancelledRestores = totalResults.Where(r => r.Cancelled);
            IEnumerable<ILibraryOperationResult> upToDateRestores = totalResults.Where(r => r.UpToDate);

            bool allSuccess = successfulRestores.Count() == totalResultsCounts;
            bool allFailed = failedRestores.Count() == totalResultsCounts;
            bool allCancelled = cancelledRestores.Count() == totalResultsCounts;
            bool allUpToDate = upToDateRestores.Count() == totalResultsCounts;
            bool partialSuccess = successfulRestores.Count() < totalResultsCounts;

            string messageText = string.Empty;

            if (allUpToDate)
            {
                messageText = LibraryManager.Resources.Text.Restore_LibrariesUptodate + Environment.NewLine;
            }
            else if (allSuccess)
            {
                string libraryId = GetLibraryId(totalResults, operation);
                messageText = GetAllSuccessString(operation, totalResultsCounts, elapsedTime, libraryId) + Environment.NewLine;
            }
            else if (allCancelled)
            {
                string libraryId = GetLibraryId(totalResults, operation);
                messageText = GetAllCancelledString(operation, totalResultsCounts, elapsedTime, libraryId) + Environment.NewLine;
            }
            else if (allFailed)
            {
                string libraryId = GetLibraryId(totalResults, operation);
                messageText = GetAllFailuresString(operation, totalResultsCounts, libraryId) + Environment.NewLine;
            }
            else
            {
                string partialSuccessText = GetPartialSuccessString(operation, successfulRestores.Count(), failedRestores.Count(), cancelledRestores.Count(), upToDateRestores.Count(), elapsedTime);
            }

            LogEvent(messageText, LogLevel.Operation);
        }

        private static string GetLibraryId(IEnumerable<ILibraryOperationResult> totalResults, OperationType operation)
        {
            if (operation == OperationType.Uninstall || operation == OperationType.Upgrade)
            {
                if (totalResults != null && totalResults.Count() == 1)
                {
                    return totalResults.First().InstallationState.LibraryId;
                }
            }

            return string.Empty;
        }

        private static string GetPartialSuccessString(OperationType operation, int successfulRestores, int failedRestores, int cancelledRestores, int upToDateRestores, TimeSpan timeSpan)
        {
            string message = string.Empty;
            message = successfulRestores > 0 ? message + string.Format(LibraryManager.Resources.Text.Restore_NumberOfLibrariesSucceeded, successfulRestores, Math.Round(timeSpan.TotalSeconds, 2)) : message;
            message = failedRestores > 0 ? message + string.Format(LibraryManager.Resources.Text.Restore_NumberOfLibrariesFailed, failedRestores, Math.Round(timeSpan.TotalSeconds, 2)) : message;
            message = cancelledRestores > 0 ? message + string.Format(LibraryManager.Resources.Text.Restore_NumberOfLibrariesCancelled, cancelledRestores, Math.Round(timeSpan.TotalSeconds, 2)) : message;

            return message;
        }

        private static string GetAllSuccessString(OperationType operation, int totalCount, TimeSpan timeSpan, string libraryId)
        {
            switch (operation)
            {
                case OperationType.Restore:
                    return string.Format(LibraryManager.Resources.Text.Restore_NumberOfLibrariesSucceeded, totalCount, Math.Round(timeSpan.TotalSeconds, 2));

                case OperationType.Clean:
                    return string.Format(LibraryManager.Resources.Text.Clean_NumberOfLibrariesSucceeded, totalCount, Math.Round(timeSpan.TotalSeconds, 2));

                case OperationType.Uninstall:
                    return string.Format(LibraryManager.Resources.Text.Uninstall_LibrarySucceeded, libraryId ?? string.Empty);

                case OperationType.Upgrade:
                    return string.Format(LibraryManager.Resources.Text.Update_LibrarySucceeded, libraryId ?? string.Empty);

                default:
                    return string.Empty;
            }
        }

        private static string GetAllFailuresString(OperationType operation, int totalCount, string libraryId)
        {
            switch (operation)
            {
                case OperationType.Restore:
                    return string.Format(LibraryManager.Resources.Text.Restore_NumberOfLibrariesFailed, totalCount);

                case OperationType.Clean:
                    return string.Format(LibraryManager.Resources.Text.Clean_NumberOfLibrariesFailed, totalCount);

                case OperationType.Uninstall:
                    return string.Format(LibraryManager.Resources.Text.Uninstall_LibraryFailed, libraryId ?? string.Empty);

                case OperationType.Upgrade:
                    return string.Format(LibraryManager.Resources.Text.Update_LibraryFailed, libraryId ?? string.Empty);

                default:
                    return string.Empty;
            }
        }

        private static string GetAllCancelledString(OperationType operation, int totalCount, TimeSpan timeSpan, string libraryId)
        {
            switch (operation)
            {
                case OperationType.Restore:
                    return string.Format(LibraryManager.Resources.Text.Restore_NumberOfLibrariesCancelled, totalCount, Math.Round(timeSpan.TotalSeconds, 2));

                case OperationType.Clean:
                    return string.Format(LibraryManager.Resources.Text.Clean_NumberOfLibrariesCancelled, totalCount, Math.Round(timeSpan.TotalSeconds, 2));

                case OperationType.Uninstall:
                    return string.Format(LibraryManager.Resources.Text.Uninstall_LibraryCancelled, libraryId ?? string.Empty);

                case OperationType.Upgrade:
                    return string.Format(LibraryManager.Resources.Text.Update_LibraryCancelled, libraryId ?? string.Empty);

                default:
                    return string.Empty;
            }
        }

        private static string GetOperationHeaderString(OperationType operationType, string libraryId)
        {
            switch (operationType)
            {
                case OperationType.Restore:
                    return LibraryManager.Resources.Text.Restore_OperationStarted;

                case OperationType.Clean:
                    return LibraryManager.Resources.Text.Clean_OperationStarted;

                case OperationType.Uninstall:
                    return string.Format(LibraryManager.Resources.Text.Uninstall_LibraryStarted, libraryId ?? string.Empty);

                case OperationType.Upgrade:
                    return string.Format(LibraryManager.Resources.Text.Update_LibraryStarted, libraryId ?? string.Empty);

                default:
                    return string.Empty;
            }
        }

        private static string GetSummaryHeaderString(OperationType operationType, string libraryId)
        {
            switch (operationType)
            {
                case OperationType.Restore:
                    return LibraryManager.Resources.Text.Restore_OperationCompleted;

                case OperationType.Clean:
                    return LibraryManager.Resources.Text.Clean_OperationCompleted;

                case OperationType.Uninstall:
                    return string.Format(LibraryManager.Resources.Text.Uninstall_LibrarySucceeded, libraryId ?? string.Empty);

                case OperationType.Upgrade:
                    return string.Format(LibraryManager.Resources.Text.Update_LibrarySucceeded, libraryId ?? string.Empty);

                default:
                    return string.Empty;
            }
        }
    }
}
