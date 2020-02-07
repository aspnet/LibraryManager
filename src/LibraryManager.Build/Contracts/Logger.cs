// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

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

        public void Log(string message, LibraryManager.Contracts.LogLevel level)
        {
            switch (level)
            {
                case LibraryManager.Contracts.LogLevel.Error:
                    Errors.Add(message);
                    break;
                case LibraryManager.Contracts.LogLevel.Operation:
                case LibraryManager.Contracts.LogLevel.Task:
                case LibraryManager.Contracts.LogLevel.Status:
                    Messages.Add(message);
                    break;
            }
        }
    }
}
