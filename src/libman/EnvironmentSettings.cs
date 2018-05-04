// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Tools.Contracts;

namespace Microsoft.Web.LibraryManager.Tools
{
    internal class EnvironmentSettings
    {
        private const string _libmanJsonFileName = "libman.json";

        public IInputReader InputReader { get; set; }
        public ILogger Logger { get; set; }
        public string CurrentWorkingDirectory { get; set; }
        public string CacheDirectory { get; set; }

        public string ManifestFileName => Path.Combine(CurrentWorkingDirectory, _libmanJsonFileName);


        public static EnvironmentSettings Default { get; } = DefaultEnvironmentSettings();

        private static EnvironmentSettings DefaultEnvironmentSettings()
        {
            return new EnvironmentSettings()
            {
                Logger = ConsoleLogger.Instance,
                InputReader = ConsoleLogger.Instance,
                CurrentWorkingDirectory = Directory.GetCurrentDirectory(),
                CacheDirectory = Constants.CacheFolder
            };
        }
    }
}
