// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Tools.Test
{
    internal class TestLogger : ILogger
    {
        public List<KeyValuePair<LogLevel, string>> Messages { get; } = new List<KeyValuePair<LogLevel, string>>();

        public void Log(string message, LogLevel level)
        {
            Messages.Add(new KeyValuePair<LogLevel, string>(level, message));
        }

        public void Clear()
        {
            Messages.Clear();
        }
    }
}
