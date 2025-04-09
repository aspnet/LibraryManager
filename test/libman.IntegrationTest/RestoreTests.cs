// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Cli.IntegrationTest;

[TestClass]
public class RestoreTests : CliTestBase
{
    [TestMethod]
    public async Task Restore_WithFileMapping_ConcreteFileNames()
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

        await ExecuteCliToolAsync("restore");

        AssertFileExists("wwwroot/lib/jquery/dist/jquery.min.js");
        AssertFileExists("wwwroot/lib/jquery/dist/jquery.min.map");
    }

    [TestMethod]
    public async Task Restore_WithFileMapping_FileGlobs()
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
                                    "dist/jquery.min.*",
                                ]
                            }
                        ]
                    }
                ]
            }
            """;
        await CreateManifestFileAsync(manifest);

        await ExecuteCliToolAsync("restore");

        AssertFileExists("wwwroot/lib/jquery/dist/jquery.min.js");
        AssertFileExists("wwwroot/lib/jquery/dist/jquery.min.map");
    }

    [TestMethod]
    public async Task Restore_WithFileMapping_FileGlobs_SetRootPath()
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
                                "root": "dist",
                                "files": [
                                    "jquery.min.*",
                                ]
                            }
                        ]
                    }
                ]
            }
            """;
        await CreateManifestFileAsync(manifest);

        await ExecuteCliToolAsync("restore");

        AssertFileExists("wwwroot/lib/jquery/jquery.min.js");
        AssertFileExists("wwwroot/lib/jquery/jquery.min.map");
    }
}
