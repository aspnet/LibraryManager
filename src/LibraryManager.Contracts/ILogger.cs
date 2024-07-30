﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Web.LibraryManager.Contracts
{
    /// <summary>
    /// Represents a logger used by the <see cref="IProvider"/> to log messages and exceptions.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs the specified message to the host.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The level of the message.</param>
        void Log(string message, LogLevel level);
    }

    /// <summary>
    /// The logging level is used to determine where to log the message
    /// </summary>
    public enum LogLevel
    {
        /// <summary>An error may or may not be shown diretly to the user.</summary>
        Error,

        /// <summary>An operation happens by the internal workings of the providers.</summary>
        Operation,

        /// <summary>A task is manually invoked by the user.</summary>
        Task,

        /// <summary>Status is a short message to display to the user.</summary>
        Status
    }
}
