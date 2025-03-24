// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Build.IntegrationTest;

[TestClass]
public class RestoreTests : BuildTestBase
{
    [TestMethod]
    public async Task Restore_LibraryWithFileMapping_NamedFiles()
    {
        string manifest = """
            {
                "version": "3.0",
                "defaultProvider": "jsdelivr",
                "libraries": [
                    {
                        "library": "jquery@3.6.0",
                        "destination": "wwwroot/lib/jquery",
                        "fileMappings": [
                            {
                                "files": [
                                    "dist/jquery.min.js",
                                    "dist/jquery.min.map"
                                ]
                            }
                        ]
                    }
                ]
            }
            """;
        await CreateManifestFileAsync(manifest);

        await RunDotnetCommandLineAsync("build", TestProjectDirectory);

        AssertFileExists("wwwroot/lib/jquery/dist/jquery.min.js");
        AssertFileExists("wwwroot/lib/jquery/dist/jquery.min.map");
    }
}
