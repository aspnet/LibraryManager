﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Tools.Contracts;

namespace Microsoft.Web.LibraryManager.Tools
{
    /// <summary>
    /// Libman host environment
    /// </summary>
    internal interface IHostEnvironment
    {
        /// <summary>
        /// Provides a input reader to get user input.
        /// </summary>
        IInputReader InputReader { get; set; }

        /// <summary>
        /// Provides a logger to display messages to user.
        /// </summary>
        ILogger Logger { get; set; }

        /// <summary>
        /// Provides the host interactions to expose host specific operations.
        /// </summary>
        IHostInteractionInternal HostInteraction { get; set; }

        /// <summary>
        /// Provides Environment Settings for libman operations.
        /// </summary>
        EnvironmentSettings EnvironmentSettings { get; }

        /// <summary>
        /// Directory where 'libman' global tool is installed.
        /// </summary>
        string ToolInstallationDir { get; }

        /// <summary>
        /// Allows updating the working directory for library.
        /// </summary>
        /// <param name="directory"></param>
        void UpdateWorkingDirectory(string directory);
    }
}
