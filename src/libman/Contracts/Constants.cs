// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Web.LibraryManager.Tools.Contracts
{
    /// <summary>
    /// Defines constants for libman.
    /// </summary>
    internal static class Constants
    {
        public const string ConfigFileName = "libman.json";
        public const string TelemetryNamespace = "webtools/librarymanager/";

        /// <summary>
        /// Defines the cache folder to use for libman
        /// </summary>
        public static string CacheFolder
        {
            get
            {
                string envVar = "%HOME%";

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    envVar = "%USERPROFILE%";
                }

                return Path.Combine(Environment.ExpandEnvironmentVariables(envVar), ".librarymanager");
            }
        }
    }
}
