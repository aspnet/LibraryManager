// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Web.LibraryManager.Tools.Test
{
    internal class TestEnvironmentHelper
    {
        public static EnvironmentSettings GetTestSettings(string workingDirectory, string cacheDirectory)
        {
            return new EnvironmentSettings()
            {
                Logger = new TestLogger(),
                InputReader = new TestInputReader(),
                CurrentWorkingDirectory = workingDirectory,
                CacheDirectory = cacheDirectory
            };
        }

        public static IHostEnvironment GetTestHostEnvironment(string workingDirectory, string cacheDirectory)
        {
            return new Mocks.HostEnvironment(GetTestSettings(workingDirectory, cacheDirectory));
        }
    }
}
