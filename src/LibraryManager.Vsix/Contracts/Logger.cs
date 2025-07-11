// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Logging;
using Microsoft.Web.LibraryManager.Vsix.Shared;

namespace Microsoft.Web.LibraryManager.Vsix.Contracts
{
    internal static class Logger
    {
        private static Guid OutputPaneGuid = new Guid("cce35aef-ace6-4371-b1e1-8efa3cdc8324");
        private static IVsOutputWindowPane OutputWindowPaneValue;
        private static IVsOutputWindow OutputWindowValue;
        private static IVsActivityLog ActivityLogValue;
        private static IVsStatusbar StatusbarValue;
        private static OutputWindowTextWriter OutputWriterValue;

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
                Telemetry.TrackException(nameof(LogEvent), ex);
                System.Diagnostics.Debug.Write(ex);
            }
        }

        /// <summary>
        /// Logs the header of the summary of an operation
        /// </summary>
        /// <param name="operationType"></param>
        /// <param name="libraryId"></param>
        public static void LogEventsHeader(OperationType operationType, string libraryId)
        {
            LogEvent(LogMessageGenerator.GetOperationHeaderString(operationType, libraryId), LogLevel.Task);
        }

        /// <summary>
        /// Logs the footer message of the summary of an operation
        /// </summary>
        /// <param name="operationType"></param>
        /// <param name="elapsedTime"></param>
        public static void LogEventsFooter(OperationType operationType, TimeSpan elapsedTime)
        {
            LogEvent(string.Format(LibraryManager.Resources.Text.TimeElapsed, elapsedTime), LogLevel.Operation);
            LogEvent(LibraryManager.Resources.Text.SummaryEndLine + Environment.NewLine, LogLevel.Operation);
        }

        /// <summary>
        /// Logs the summary messages for a given <see cref="OperationType"/>
        /// </summary>
        /// <param name="totalResults"></param>
        /// <param name="operationType"></param>
        /// <param name="elapsedTime"></param>
        /// <param name="endOfMessage"></param>
        public static void LogEventsSummary(IEnumerable<OperationResult<LibraryInstallationGoalState>> totalResults, OperationType operationType, TimeSpan elapsedTime, bool endOfMessage = true)
        {
            LogErrors(totalResults);
            LogEvent(LogMessageGenerator.GetSummaryHeaderString(operationType), LogLevel.Task);
            LogOperationSummary(totalResults, operationType, elapsedTime);

            if (endOfMessage)
            {
                LogEventsFooter(operationType, elapsedTime);
            }
        }

        /// <summary>
        /// Logs errors messages for a given <see cref="OperationType"/>
        /// </summary>
        /// <param name="errorMessages">Messages to be logged</param>
        /// <param name="operationType"><see cref="OperationType"/></param>
        /// <param name="endOfMessage">Whether or not to log end of message lines</param>
        public static void LogErrorsSummary(IEnumerable<string> errorMessages, OperationType operationType, bool endOfMessage = true)
        {
            foreach (string error in errorMessages)
            {
                LogEvent(error, LogLevel.Operation);
            }

            LogEvent(LogMessageGenerator.GetErrorsHeaderString(operationType), LogLevel.Task);

            if (endOfMessage)
            {
                LogEvent(LibraryManager.Resources.Text.SummaryEndLine + Environment.NewLine, LogLevel.Operation);
            }
        }

        /// <summary>
        /// Logs errors messages for a given <see cref="OperationType"/>
        /// </summary>
        /// <param name="results">Operation results</param>
        /// <param name="operationType"><see cref="OperationType"/></param>
        /// <param name="endOfMessage">Whether or not to log end of message lines</param>
        public static void LogErrorsSummary(IEnumerable<OperationResult<LibraryInstallationGoalState>> results, OperationType operationType, bool endOfMessage = true)
        {
            List<string> errorStrings = GetErrorStrings(results);
            LogErrorsSummary(errorStrings, operationType, endOfMessage);
        }

        public static void ClearOutputWindow()
        {
            _ = ClearOutputWindowAsync();
        }

        private static async Task ClearOutputWindowAsync()
        {
            // Don't access _outputWindowPane through the property here so that we don't force creation
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            OutputWindowPaneValue?.Clear();
        }

        private static IVsOutputWindowPane OutputWindowPane
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread(nameof(OutputWindowPane));

                if (OutputWindowPaneValue == null)
                {
                    EnsurePane();
                }

                return OutputWindowPaneValue;
            }
        }

        private static OutputWindowTextWriter OutputWriter
        {
            get
            {
                if (OutputWriterValue is null)
                {
                    ThreadHelper.JoinableTaskFactory.Run(async () =>
                        {
                            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                            if (OutputWriterValue is not null)
                            {
                                // This checks that no other requests have initialized the writer while
                                // we waited the switch threads.
                                // Since we're now running on the UI thread, we don't need a lock - multiple
                                // attempts will run serially.
                                return;
                            }

                            // this needs to be on the UI thread
                            EnsurePane();

                            // needs to be initialized on UI thread.
                            // Internal bug: https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1595387
                            OutputWriterValue = new OutputWindowTextWriter(OutputWindowPaneValue);
                        });
                }

                return OutputWriterValue;
            }
        }

        private static IVsOutputWindow OutputWindow
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread(nameof(OutputWindow));

                if (OutputWindowValue == null)
                {
                    OutputWindowValue = VsHelpers.GetService<SVsOutputWindow, IVsOutputWindow>();
                }

                return OutputWindowValue;
            }
        }

        private static IVsActivityLog ActivityLog
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread(nameof(ActivityLog));

                if (ActivityLogValue == null)
                {
                    ActivityLogValue = VsHelpers.GetService<SVsActivityLog, IVsActivityLog>();
                }

                return ActivityLogValue;
            }
        }

        private static IVsStatusbar Statusbar
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread(nameof(Statusbar));

                if (StatusbarValue == null)
                {
                    StatusbarValue = VsHelpers.GetService<SVsStatusbar, IVsStatusbar>();
                }

                return StatusbarValue;
            }
        }

        private static void LogToActivityLog(string message, __ACTIVITYLOG_ENTRYTYPE type)
        {
            _ = LogToActivityLogAsync(message, type);
        }

        private static async Task LogToActivityLogAsync(string message, __ACTIVITYLOG_ENTRYTYPE type)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            ActivityLog.LogEntry((uint)type, Vsix.Name, message);
        }

        private static void LogToStatusBar(string message)
        {
            _ = LogToStatusBarAsync(message);
        }

        private static async Task LogToStatusBarAsync(string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            Statusbar.FreezeOutput(0);
            Statusbar.SetText(message);
            Statusbar.FreezeOutput(1);
        }

        private static void LogToOutputWindow(object message)
        {
            OutputWriter.WriteLine(message);
        }

        private static bool EnsurePane()
        {
            ThreadHelper.ThrowIfNotOnUIThread(nameof(EnsurePane));

            if (OutputWindowPaneValue == null)
            {
                if (OutputWindow != null)
                {
                    if (ErrorHandler.Failed(OutputWindow.GetPane(ref OutputPaneGuid, out OutputWindowPaneValue)) &&
                        ErrorHandler.Succeeded(OutputWindow.CreatePane(ref OutputPaneGuid, Resources.Text.OutputWindowTitle, 0, 0)))
                    {
                        if (ErrorHandler.Succeeded(OutputWindow.GetPane(ref OutputPaneGuid, out OutputWindowPaneValue)))
                        {
                            OutputWindowPaneValue.Activate();
                        }
                    }
                }
            }

            return OutputWindowPaneValue != null;
        }

        private static void LogOperationSummary(IEnumerable<OperationResult<LibraryInstallationGoalState>> totalResults, OperationType operation, TimeSpan elapsedTime)
        {
            string messageText = LogMessageGenerator.GetOperationSummaryString(totalResults, operation, elapsedTime);

            if (!string.IsNullOrEmpty(messageText))
            {
                LogEvent(messageText, LogLevel.Operation);
            }
        }

        private static void LogErrors(IEnumerable<OperationResult<LibraryInstallationGoalState>> results)
        {
            foreach (OperationResult<LibraryInstallationGoalState> result in results)
            {
                foreach (IError error in result.Errors)
                {
                    LogEvent(error.Message, LogLevel.Operation);
                }
            }
        }

        private static List<string> GetErrorStrings(IEnumerable<OperationResult<LibraryInstallationGoalState>> results)
        {
            List<string> errorStrings = new List<string>();

            foreach (OperationResult<LibraryInstallationGoalState> result in results)
            {
                foreach (IError error in result.Errors)
                {
                    errorStrings.Add(error.Message);
                }
            }

            return errorStrings;
        }
    }
}
