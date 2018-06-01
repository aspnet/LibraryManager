// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
