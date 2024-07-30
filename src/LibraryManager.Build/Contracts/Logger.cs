// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using LogLevel = Microsoft.Web.LibraryManager.Contracts.LogLevel;

namespace Microsoft.Web.LibraryManager.Build.Contracts
{
    internal class Logger : LibraryManager.Contracts.ILogger
    {
        private Logger()
        {
        }

        public static Logger Instance { get; } = new Logger();
        public ICollection<string> Messages { get; } = new List<string>();
        public ICollection<string> Errors { get; } = new List<string>();

        public void Clear()
        {
            Messages.Clear();
            Errors.Clear();
        }

        public void Log(string message, LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Error:
                    Errors.Add(message);
                    break;
                case LogLevel.Operation:
                case LogLevel.Task:
                case LogLevel.Status:
                    Messages.Add(message);
                    break;
            }
        }
    }
}
