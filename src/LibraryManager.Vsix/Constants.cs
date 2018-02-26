// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.Web.LibraryManager.Vsix
{
    public static class Constants
    {
        public const string ConfigFileName = "library.json";
        public const string TelemetryNamespace = "webtools/librarymanager/";
        public const string MainNuGetPackageId = "Microsoft.Web.LibraryManager.Build";
        public const string ErrorCodeLink = "https://github.com/aspnet/LibraryManager/wiki/Error-codes#{0}";

        public static string CacheFolder
        {
            get
            {
                string envVar = "%HOME%";

                if (Path.DirectorySeparatorChar == '\\') // Windows
                {
                    envVar = "%USERPROFILE%";
                }

                return Path.Combine(Environment.ExpandEnvironmentVariables(envVar), ".librarymanager");
            }
        }
    }
}
