// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Build.IntegrationTest;

[TestClass]
[DeploymentItem(@"TestPackages", "TestPackages")]
[DeploymentItem(@"TestSolution", "TestSolution")]
public class BuildTestBase
{
    private const string BuildPackageName = "Microsoft.Web.LibraryManager.Build";
    private const string ManifestFileName = "libman.json";
    private const string TestProjectFolderName = "Libman.Build.TestApp";
    private readonly string PackagesFolderPath = Path.Combine(Environment.CurrentDirectory, "TestPackages");
    protected string TestProjectDirectory { get; private set; } = "";

    [TestInitialize]
    public async Task TestInitialize()
    {
        // test solution should be deployed with every test?
        TestProjectDirectory = Path.Combine(Path.GetFullPath("TestSolution"), TestProjectFolderName);

        // create an empty nuget.config with only our package source
        await AddPackageReferenceAsync();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(TestProjectDirectory))
        {
            Directory.Delete(TestProjectDirectory, true);
        }
    }

    protected async Task RunDotnetCommandLineAsync(string arguments, string? workingDirectory)
    {
        var processStartInfo = new ProcessStartInfo("dotnet", arguments)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory,
        };

        using (var process = Process.Start(processStartInfo))
        {
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                string output = await process.StandardError.ReadToEndAsync() + await process.StandardOutput.ReadToEndAsync();
                throw new InvalidOperationException($"CLI tool execution failed with arguments: {arguments}.\r\nOutput: {output}");
            }
        }
    }

    private async Task AddPackageReferenceAsync()
    {
        await RunDotnetCommandLineAsync($"nuget add source \"{PackagesFolderPath}\"", TestProjectDirectory);

        await RunDotnetCommandLineAsync($"add package {BuildPackageName} --version {GetBuildPackageVersion()}", TestProjectDirectory);
    }

    private static string GetBuildPackageVersion()
    {
        return Directory.GetFiles("TestPackages", $"{BuildPackageName}.*.nupkg")
            .Select(Path.GetFileNameWithoutExtension)
            .Select(name => name.Substring(BuildPackageName.Length + 1))
            .OrderByDescending(version => version)
            .First();
    }

    protected async Task CreateManifestFileAsync(string content)
    {
        string manifestFilePath = Path.Combine(TestProjectDirectory, ManifestFileName);
        await Task.Run(() => File.WriteAllText(manifestFilePath, content));
    }

    protected void AssertFileExists(string relativeFilePath)
    {
        string filePath = Path.Combine(TestProjectDirectory, relativeFilePath);
        Assert.IsTrue(File.Exists(filePath), $"Expected file '{filePath}' does not exist.");
    }

}
