// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Tools.Contracts;

namespace Microsoft.Web.LibraryManager.Tools
{
    internal class HostEnvironment : IHostEnvironment
    {
        public static HostEnvironment Instance { get; private set; }

        private HostEnvironment(EnvironmentSettings settings)
        {
            EnvironmentSettings = settings ?? throw new ArgumentNullException(nameof(settings));
        }


        public IInputReader InputReader { get; set; }
        public ILogger Logger { get; set; }
        public IHostInteractionInternal HostInteraction { get; set; }
        public EnvironmentSettings EnvironmentSettings { get; }
        public string ToolInstallationDir { get; } = Path.GetDirectoryName(typeof(HostEnvironment).Assembly.Location);


        static object _syncObj = new object();
        public static HostEnvironment Initialize(EnvironmentSettings settings)
        {
            lock (_syncObj)
            {
                Instance = new HostEnvironment(settings);

                Instance.InputReader = ConsoleLogger.Instance;
                Instance.Logger = ConsoleLogger.Instance;

                Instance.HostInteraction = new HostInteraction(settings);

                return Instance;
            }
        }

        public void UpdateWorkingDirectory(string directory)
        {
            EnvironmentSettings.CurrentWorkingDirectory = directory;
            HostInteraction.UpdateWorkingDirectory(directory);
        }
    }
}
