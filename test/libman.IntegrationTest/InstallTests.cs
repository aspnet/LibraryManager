// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Cli.IntegrationTest;

[TestClass]
public class InstallTests : CliTestBase
{
    [TestMethod]
    public async Task Install_FileSpecified()
    {
        await ExecuteCliToolAsync("install jquery@3.6.0 --provider cdnjs --destination test/jquery --files jquery.min.js");

        AssertFileExists("test/jquery/jquery.min.js");
    }

    [TestMethod]
    public async Task Install_UsingTemplateInDefaultDestination()
    {
        string manifest = """
            {
                "version": "3.0",
                "defaultProvider": "cdnjs",
                "defaultDestination": "wwwroot/lib/[Name]/"
            }
            """;
        await CreateManifestFileAsync(manifest);

        await ExecuteCliToolAsync("install bootstrap@5.3.2 --provider cdnjs --files css/bootstrap.min.css --files js/bootstrap.bundle.min.js");
        AssertFileExists("wwwroot/lib/bootstrap/css/bootstrap.min.css");
        AssertFileExists("wwwroot/lib/bootstrap/js/bootstrap.bundle.min.js");
    }
}
