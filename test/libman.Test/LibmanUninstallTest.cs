// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Tools.Commands;

namespace Microsoft.Web.LibraryManager.Tools.Test
{
    [TestClass]
    public class LibmanUninstallTest : CommandTestBase
    {
        [TestInitialize]
        public override void Setup()
        {
            base.Setup();
        }

        [TestCleanup]
        public override void Cleanup()
        {
            base.Cleanup();
        }

        [TestMethod]
        public void TestUninstall()
        {
            var command = new UninstallCommand(HostEnvironment);
            command.Configure(null);

            string contents = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""defaultDestination"": ""wwwroot"",
  ""libraries"": [
    {
      ""library"": ""jquery@3.2.1"",
      ""files"": [ ""jquery.min.js"", ""core.js"" ]
    }
  ]
}";

            string libmanjsonPath = Path.Combine(WorkingDir, "libman.json");
            File.WriteAllText(libmanjsonPath, contents);

            var restoreCommand = new RestoreCommand(HostEnvironment);
            restoreCommand.Configure(null);

            restoreCommand.Execute();

            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.min.js")));
            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "core.js")));

            int result = command.Execute("jquery@3.2.1");

            Assert.AreEqual(0, result);
            Assert.IsFalse(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.min.js")));
            Assert.IsFalse(File.Exists(Path.Combine(WorkingDir, "wwwroot", "core.js")));

            string expectedText = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""defaultDestination"": ""wwwroot"",
  ""libraries"": []
}";
            string actualText = File.ReadAllText(libmanjsonPath);

            Assert.AreEqual(StringHelper.NormalizeNewLines(expectedText), StringHelper.NormalizeNewLines(actualText));
        }

        [TestMethod]
        public void TestUninstall_NoLibraryToUninstall()
        {
            var command = new UninstallCommand(HostEnvironment);
            command.Configure(null);

            string contents = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""defaultDestination"": ""wwwroot"",
  ""libraries"": [
    {
      ""provider"": ""cdnjs"",
      ""library"": ""jquery@3.2.1"",
      ""destination"": ""wwwroot"",
      ""files"": [
        ""jquery.min.js"",
        ""core.js""
      ]
    }
  ]
}";

            string libmanjsonPath = Path.Combine(WorkingDir, "libman.json");
            File.WriteAllText(libmanjsonPath, contents);

            var restoreCommand = new RestoreCommand(HostEnvironment);
            restoreCommand.Configure(null);

            restoreCommand.Execute();

            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.min.js")));
            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "core.js")));

            int result = command.Execute("jquery@2.2.1");

            Assert.AreEqual(0, result);
            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.min.js")));
            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "core.js")));

            var logger = HostEnvironment.Logger as TestLogger;

            Assert.AreEqual("Library \"jquery@2.2.1\" is not installed. Nothing to uninstall", logger.Messages[logger.Messages.Count-1].Value);

            string actualText = File.ReadAllText(libmanjsonPath);

            Assert.AreEqual(StringHelper.NormalizeNewLines(contents), StringHelper.NormalizeNewLines(actualText));
        }
    }
}
