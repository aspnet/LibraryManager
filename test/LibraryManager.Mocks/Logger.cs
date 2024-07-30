// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Web.LibraryManager.Contracts;
using System;
using System.Collections.Generic;

namespace Microsoft.Web.LibraryManager.Mocks
{
    /// <summary>
    /// A mock of the <see cref="ILogger"/> interface.
    /// </summary>
    /// <seealso cref="LibraryManager.Contracts.ILogger" />
    public class Logger : ILogger
    {
        /// <summary>
        /// A list of all the log messages recorded by the <see cref="Log"/> method.
        /// </summary>
        public List<Tuple<string, LogLevel>> Messages = new List<Tuple<string, LogLevel>>();

        /// <summary>
        /// Logs the specified message to the host.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The level of the message.</param>
        public virtual void Log(string message, LogLevel level)
        {
            Messages.Add(Tuple.Create(message, level));
        }
    }
}
