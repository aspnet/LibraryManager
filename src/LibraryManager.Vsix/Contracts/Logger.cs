// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
    }
}
