// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Tools.Contracts;

namespace Microsoft.Web.LibraryManager.Tools
{
    /// <inheritdoc />
    internal class HostEnvironment : IHostEnvironment
    {
        /// <summary>
        /// Instance of HostEnvironment to be used by libman
        /// </summary>
        public static HostEnvironment Instance { get; private set; }

        private HostEnvironment(EnvironmentSettings settings)
        {
            EnvironmentSettings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <inheritdoc />
        public IInputReader InputReader { get; set; }

        /// <inheritdoc />
        public ILogger Logger { get; set; }

        /// <inheritdoc />
        public IHostInteractionInternal HostInteraction { get; set; }

        /// <inheritdoc />
        public EnvironmentSettings EnvironmentSettings { get; }

        /// <inheritdoc />
        public string ToolInstallationDir { get; } = Path.GetDirectoryName(typeof(HostEnvironment).Assembly.Location);


        static readonly object SyncObj = new object();

        /// <summary>
        /// Initiliazes the HostEnvironment using the <paramref name="settings"/>
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static HostEnvironment Initialize(EnvironmentSettings settings)
        {
            lock (SyncObj)
            {
                Instance = new HostEnvironment(settings);

                Instance.InputReader = settings.InputReader;
                Instance.Logger = settings.Logger;

                Instance.HostInteraction = new HostInteraction(settings);

                return Instance;
            }
        }

        /// <inheritdoc />
        public void UpdateWorkingDirectory(string directory)
        {
            EnvironmentSettings.CurrentWorkingDirectory = directory;
            HostInteraction.UpdateWorkingDirectory(directory);
        }
    }
}
