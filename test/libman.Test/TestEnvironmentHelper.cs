// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
