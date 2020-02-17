// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
        public static void LogEventsSummary(IEnumerable<ILibraryOperationResult> totalResults, OperationType operationType, TimeSpan elapsedTime, bool endOfMessage = true)
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
        public static void LogErrorsSummary(IEnumerable<ILibraryOperationResult> results, OperationType operationType, bool endOfMessage = true)
        {
            List<string> errorStrings = GetErrorStrings(results);
            LogErrorsSummary(errorStrings, operationType, endOfMessage);
        }

        public static void ClearOutputWindow()
        {
            // Don't access _outputWindowPane through the property here so that we don't force creation
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                OutputWindowPaneValue?.Clear();
            });
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
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                ActivityLog.LogEntry((uint)type, Vsix.Name, message);
            });
        }

        private static void LogToStatusBar(string message)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                Statusbar.FreezeOutput(0);
                Statusbar.SetText(message);
                Statusbar.FreezeOutput(1);
            });
        }

        private static void LogToOutputWindow(object message)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                OutputWindowPane?.OutputString(message + Environment.NewLine);
            });
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

        private static void LogOperationSummary(IEnumerable<ILibraryOperationResult> totalResults, OperationType operation, TimeSpan elapsedTime)
        {
            string messageText = LogMessageGenerator.GetOperationSummaryString(totalResults, operation, elapsedTime);

            if (!string.IsNullOrEmpty(messageText))
            {
                LogEvent(messageText, LogLevel.Operation);
            }
        }

        private static void LogErrors(IEnumerable<ILibraryOperationResult> results)
        {
            foreach (ILibraryOperationResult result in results)
            {
                foreach (IError error in result.Errors)
                {
                    LogEvent(error.Message, LogLevel.Operation);
                }
            }
        }

        private static List<string> GetErrorStrings(IEnumerable<ILibraryOperationResult> results)
        {
            List<string> errorStrings = new List<string>();

            foreach (ILibraryOperationResult result in results)
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
