// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace LibraryInstaller.Vsix
{
    public class Constants
    {
        public const string ConfigFileName = "library.json";
        public static string CacheFolder = Environment.ExpandEnvironmentVariables(@"%localappdata%\Microsoft\Library\");
        public const string TelemetryNamespace = "webtools/libraryinstaller/";
    }
}
