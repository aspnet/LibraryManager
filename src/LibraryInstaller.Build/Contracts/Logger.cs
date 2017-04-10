// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryInstaller.Contracts;
using Microsoft.Build.Utilities;

namespace Microsoft.Web.LibraryInstaller.Build
{
    public class Logger : ILogger
    {
        private Task _task;

        public Logger(Task task)
        {
            _task = task;
        }

        public void Log(string message, LogLevel level)
        {
            _task.Log.LogMessage(Microsoft.Build.Framework.MessageImportance.High, message);
        }
    }
}
