// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Web.LibraryManager.Cache;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Tools.Contracts;

namespace Microsoft.Web.LibraryManager.Tools
{
    /// <summary>
    /// Defines the environment settings for the libman tool
    /// </summary>
    internal class EnvironmentSettings
    {
        private const string LibmanJsonFileName = "libman.json";

        /// <summary>
        /// Input Reader for getting user inputs.
        /// </summary>
        public IInputReader InputReader { get; set; }

        /// <summary>
        /// Logger to display messages to the user.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Directory in which libman operations are performed.
        /// </summary>
        public string CurrentWorkingDirectory { get; set; }

        /// <summary>
        /// Cache directory for libman.
        /// </summary>
        public string CacheDirectory { get; set; }

        /// <summary>
        /// Full path of libman.json in the current working directory.
        /// </summary>
        public string ManifestFileName => Path.Combine(CurrentWorkingDirectory, LibmanJsonFileName);

        public string DefaultProvider { get; set; }

        public string DefaultDestinationRoot =>
            Directory.Exists(Path.Combine(CurrentWorkingDirectory, "wwwroot"))
                ? Path.Combine("wwwroot", "lib")
                : "lib";

        /// <summary>
        /// The default environment settings for libman.
        /// </summary>
        public static EnvironmentSettings Default { get; } = DefaultEnvironmentSettings();

        private static EnvironmentSettings DefaultEnvironmentSettings()
        {
            return new EnvironmentSettings()
            {
                Logger = ConsoleLogger.Instance,
                InputReader = ConsoleLogger.Instance,
                CurrentWorkingDirectory = Directory.GetCurrentDirectory(),
                CacheDirectory = CacheService.CacheFolder,
                DefaultProvider = "cdnjs",
            };
        }
    }
}
