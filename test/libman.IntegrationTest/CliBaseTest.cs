// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Cli.IntegrationTest;

[TestClass]
[DeploymentItem(@"TestPackages", "TestPackages")]
[DeploymentItem("TestFiles", "TestFiles")]
public class CliTestBase
{
    private const string CliPackageName = "Microsoft.Web.LibraryManager.Cli";
    private const string ToolInstallPath = "./TestInstallPath";
    private const string ManifestFileName = "libman.json";
    private string _testDirectory;

    [TestInitialize]
    public async Task TestInitialize()
    {
        // Create a test directory for the project where we'll run the tool.  This isolates it from
        // inheriting any build settings from our solution.
        _testDirectory = Path.Combine(Path.GetTempPath(), "LibmanTest" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        // Create an empty nuget.config with only our package source
        // We need to set packageSourceMappings to override the defaults.
        // This is needed because external devs may need to override the root nuget.config
        // to build (see https://github.com/aspnet/LibraryManager/issues/728), and those
        // settings are inherited in the test directory.
        string nugetConfigContent = """
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="LocalPackages" value="./TestPackages" />
              </packageSources>
              <packageSourceMapping>
                <packageSource key="LocalPackages">
                  <package pattern="Microsoft.*" />
                </packageSource>
              </packageSourceMapping>
            </configuration>
            """;
        File.WriteAllText("nuget.config", nugetConfigContent);

        // This installs the tool in the current (test) working directory, not the project directory
        // created above.
        await InstallCliToolAsync();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }

        if (File.Exists("nuget.config"))
        {
            File.Delete("nuget.config");
        }
    }

    private async Task RunDotnetCommandLineAsync(string arguments)
    {
        var processStartInfo = new ProcessStartInfo("dotnet", arguments)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using (var process = Process.Start(processStartInfo))
        {
            await WaitForExitAsync(process);
            if (process.ExitCode != 0)
            {
                string output = await process.StandardError.ReadToEndAsync() + await process.StandardOutput.ReadToEndAsync();
                throw new InvalidOperationException($"Failed to run command line `dotnet {arguments}`.\r\nOutput: {output}");
            }
        }
    }

    private async Task InstallCliToolAsync()
    {
        await RunDotnetCommandLineAsync($"tool install {CliPackageName} --no-cache --prerelease --tool-path {ToolInstallPath}");
    }

    protected async Task CreateManifestFileAsync(string content)
    {
        string manifestFilePath = Path.Combine(_testDirectory, ManifestFileName);
        await Task.Run(() => File.WriteAllText(manifestFilePath, content));
    }

    protected async Task ExecuteCliToolAsync(string arguments)
    {
        var processStartInfo = new ProcessStartInfo($"{ToolInstallPath}\\libman.exe", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = _testDirectory,
        };

        using (var process = Process.Start(processStartInfo))
        {
            await WaitForExitAsync(process);
            if (process.ExitCode != 0)
            {
                string output = await process.StandardError.ReadToEndAsync() + await process.StandardOutput.ReadToEndAsync();
                throw new InvalidOperationException($"CLI tool execution failed with arguments: {arguments}.\r\nOutput: {output}");
            }
        }
    }

    private Task WaitForExitAsync(Process process)
    {
        var tcs = new TaskCompletionSource<bool>();
        process.Exited += (sender, args) => tcs.SetResult(true);
        process.EnableRaisingEvents = true;
        if (process.HasExited && !tcs.Task.IsCompleted)
        {
            tcs.SetResult(true);
        }
        return tcs.Task;
    }

    protected void AssertFileExists(string relativeFilePath)
    {
        string filePath = Path.Combine(_testDirectory, relativeFilePath);
        Assert.IsTrue(File.Exists(filePath), $"Expected file '{relativeFilePath}' does not exist.");
    }

    protected void AssertDirectoryContents(string directoryPath, IEnumerable<string> expectedFiles, bool failOnExtraFiles = false)
    {
        string fullPath = Path.Combine(_testDirectory, directoryPath);
        Assert.IsTrue(Directory.Exists(fullPath), $"Expected directory '{directoryPath}' does not exist.");
        HashSet<string> actualFiles = Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(fullPath, file))
            .ToHashSet();

        foreach (string file in expectedFiles)
        {
            Assert.IsTrue(actualFiles.Contains(file), $"Directory contents do not match. Expected: {string.Join(", ", expectedFiles)}. Actual: {string.Join(", ", actualFiles)}");
        }

        if (failOnExtraFiles)
        {
            List<string> extraFiles = actualFiles.Except(expectedFiles).Order().ToList();
            Assert.IsFalse(extraFiles.Any(), $"Unexpected files found in directory '{directoryPath}': {string.Join(", ", extraFiles)}");
        }
    }
}
