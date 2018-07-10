// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Logging;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal static class Logger
    {
        private static Guid _outputPaneGuid = new Guid("cce35aef-ace6-4371-b1e1-8efa3cdc8324");
        private static IVsOutputWindowPane _outputWindowPane;
        private static IVsOutputWindow _outputWindow;
        private static IVsActivityLog _activityLog;
        private static IVsStatusbar _statusbar;

        private static IVsOutputWindowPane OutputWindowPane
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread(nameof(OutputWindowPane));

                if (_outputWindowPane == null)
                {
                    EnsurePane();
                }

                return _outputWindowPane;
            }
        }

        private static IVsOutputWindow OutputWindow
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread(nameof(OutputWindow));

                if (_outputWindow == null)
                {
                    _outputWindow = VsHelpers.GetService<SVsOutputWindow, IVsOutputWindow>();
                }

                return _outputWindow;
            }
        }

        private static IVsActivityLog ActivityLog
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread(nameof(ActivityLog));

                if (_activityLog == null)
                {
                    _activityLog = VsHelpers.GetService<SVsActivityLog, IVsActivityLog>();
                }

                return _activityLog;
            }
        }

        private static IVsStatusbar Statusbar
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread(nameof(Statusbar));

                if (_statusbar == null)
                {
                    _statusbar = VsHelpers.GetService<SVsStatusbar, IVsStatusbar>();
                }

                return _statusbar;
            }
        }

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

        public static void ClearOutputWindow()
        {
            // Don't access _outputWindowPane through the property here so that we don't force creation
            ThreadHelper.Generic.BeginInvoke(() => _outputWindowPane?.Clear());
        }

        private static void LogToActivityLog(string message, __ACTIVITYLOG_ENTRYTYPE type)
        {
            ThreadHelper.Generic.BeginInvoke(() => ActivityLog.LogEntry((uint)type, Vsix.Name, message));
        }

        public static void LogToStatusBar(string message)
        {
            ThreadHelper.Generic.BeginInvoke(() =>
            {
                Statusbar.FreezeOutput(0);
                Statusbar.SetText(message);
                Statusbar.FreezeOutput(1);
            });
        }

        private static void LogToOutputWindow(object message)
        {
            ThreadHelper.Generic.BeginInvoke(() => OutputWindowPane?.OutputString(message + Environment.NewLine));
        }

        private static bool EnsurePane()
        {
            ThreadHelper.ThrowIfNotOnUIThread(nameof(EnsurePane));

            if (_outputWindowPane == null)
            {
                if (OutputWindow != null)
                {
                    if (ErrorHandler.Failed(OutputWindow.GetPane(ref _outputPaneGuid, out _outputWindowPane)) &&
                        ErrorHandler.Succeeded(OutputWindow.CreatePane(ref _outputPaneGuid, Resources.Text.OutputWindowTitle, 0, 0)))
                    {
                        if (ErrorHandler.Succeeded(OutputWindow.GetPane(ref _outputPaneGuid, out _outputWindowPane)))
                        {
                            _outputWindowPane.Activate();
                        }
                    }
                }
            }

            return _outputWindowPane != null;
        }

        internal static void LogEventsHeader(OperationType operationType, string libraryId)
        {
            LogEvent(LogMessageGenerator.GetOperationHeaderString(operationType, libraryId), LogLevel.Task);
        }

        internal static void LogEventsSummary(IEnumerable<ILibraryOperationResult> totalResults, OperationType operationType, TimeSpan elapsedTime )
        {
            LogErrors(totalResults);
            LogEvent(LogMessageGenerator.GetSummaryHeaderString(operationType, null), LogLevel.Task);
            LogOperationSummary(totalResults, operationType, elapsedTime);
            LogEvent(string.Format(LibraryManager.Resources.Text.TimeElapsed, elapsedTime), LogLevel.Operation);
            LogEvent(LibraryManager.Resources.Text.SummaryEndLine + Environment.NewLine, LogLevel.Operation);
        }

        private static void LogOperationSummary(IEnumerable<ILibraryOperationResult> totalResults, OperationType operation, TimeSpan elapsedTime)
        {
            string messageText = LogMessageGenerator.GetOperationSummaryString(totalResults, operation, elapsedTime);

            if (!string.IsNullOrEmpty(messageText))
            {
                LogEvent(messageText, LogLevel.Operation);
            }
        }

        public static void LogErrors(IEnumerable<ILibraryOperationResult> results)
        {
            foreach (ILibraryOperationResult result in results)
            {
                foreach (IError error in result.Errors)
                {
                    LogEvent(error.Message, LogLevel.Operation);
                }
            }
        }
    }
}
