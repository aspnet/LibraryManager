// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Tools.Contracts;


namespace Microsoft.Web.LibraryManager.Tools.Test.Mocks
{
    internal class HostEnvironment : IHostEnvironment
    {
        public HostEnvironment(EnvironmentSettings envSettings)
        {
            EnvironmentSettings = envSettings;
            InputReader = new TestInputReader();
            Logger = new TestLogger();
            HostInteraction = new HostInteractionInternal(envSettings.CurrentWorkingDirectory, envSettings.CacheDirectory)
            {
                Logger = Logger,
            };
        }

        public IInputReader InputReader { get; set; }
        public ILogger Logger { get; set; }
        public IHostInteractionInternal HostInteraction { get; set; }

        public EnvironmentSettings EnvironmentSettings { get; }

        public string ToolInstallationDir => Path.GetDirectoryName(typeof(HostEnvironment).Assembly.Location);

        public void UpdateWorkingDirectory(string directory)
        {
            throw new NotImplementedException();
        }
    }
}
