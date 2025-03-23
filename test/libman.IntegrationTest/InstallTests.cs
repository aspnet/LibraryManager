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
}
