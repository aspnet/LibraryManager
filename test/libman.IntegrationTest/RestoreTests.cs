// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
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

    [TestMethod]
    public async Task Restore_FileSystemProvider_WithFileMapping()
    {
        string testFilesPath = Path.Combine(Environment.CurrentDirectory, "TestFiles");
        string manifest = $$"""
            {
                "version": "3.0",
                "libraries": [
                    {
                        "provider": "filesystem",
                        "library": "{{testFilesPath.Replace('\\', '/')}}",
                        "destination": "wwwroot/lib/testFiles",
                        "fileMappings": [
                            {
                                "files": [
                                    "*.min.js",
                                ]
                            }
                        ]
                    }
                ]
            }
            """;
        await CreateManifestFileAsync(manifest);

        await ExecuteCliToolAsync("restore");

        AssertDirectoryContents("wwwroot/lib/testFiles/", ["EmptyFile.min.js"], failOnExtraFiles: true);
    }

    [TestMethod]
    public async Task Restore_FileSystemProvider_WithFileRename()
    {
        string testFilesPath = Path.Combine(Environment.CurrentDirectory, "TestFiles");
        string manifest = $$"""
            {
                "version": "3.0",
                "libraries": [
                    {
                        "provider": "filesystem",
                        "library": "{{testFilesPath.Replace('\\', '/')}}/EmptyFile.min.js",
                        "destination": "wwwroot/lib/testFiles",
                        "files": [
                            "TheOnlyFile.js",
                        ]
                    }
                ]
            }
            """;
        await CreateManifestFileAsync(manifest);

        await ExecuteCliToolAsync("restore");

        AssertDirectoryContents("wwwroot/lib/testFiles/", ["TheOnlyFile.js"], failOnExtraFiles: true);
    }
}
