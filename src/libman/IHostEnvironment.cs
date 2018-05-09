// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Tools.Contracts;

namespace Microsoft.Web.LibraryManager.Tools
{
    internal interface IHostEnvironment
    {
        IInputReader InputReader { get; set; }
        ILogger Logger { get; set; }
        IHostInteractionInternal HostInteraction { get; set; }
        EnvironmentSettings EnvironmentSettings { get; }
        string ToolInstallationDir { get; }
        void UpdateWorkingDirectory(string directory);
    }
}
