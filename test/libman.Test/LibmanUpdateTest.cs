// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Tools.Commands;

namespace Microsoft.Web.LibraryManager.Tools.Test
{
    [TestClass]
    public class LibmanUpdateTest : CommandTestBase
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
        public void TestUpdateCommand()
        {
            var command = new UpdateCommand(HostEnvironment);
            command.Configure(null);

            string contents = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""defaultDestination"": ""wwwroot"",
  ""libraries"": [
    {
      ""library"": ""jquery@2.2.0"",
      ""files"": [ ""jquery.min.js"", ""jquery.js"" ]
    }
  ]
}";

            string libmanjsonPath = Path.Combine(WorkingDir, "libman.json");
            File.WriteAllText(libmanjsonPath, contents);

            var restoreCommand = new RestoreCommand(HostEnvironment);
            restoreCommand.Configure(null);

            restoreCommand.Execute();

            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.min.js")));
            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.js")));

            int result = command.Execute("jquery", "--to", "3.5.0");

            Assert.AreEqual(0, result);
            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.min.js")));
            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.js")));

            string actualText = File.ReadAllText(libmanjsonPath);

            string expectedText = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""defaultDestination"": ""wwwroot"",
  ""libraries"": [
    {
      ""library"": ""jquery@3.5.0"",
      ""files"": [
        ""jquery.min.js"",
        ""jquery.js""
      ]
    }
  ]
}";
            Assert.AreEqual(StringHelper.NormalizeNewLines(expectedText), StringHelper.NormalizeNewLines(actualText));
        }

        [TestMethod]
        public void TestUpdateCommand_InvalidLibraryName()
        {
            var command = new UpdateCommand(HostEnvironment);
            command.Configure(null);

            string contents = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""defaultDestination"": ""wwwroot"",
  ""libraries"": [
    {
      ""library"": ""jquery@2.2.0"",
      ""files"": [
        ""jquery.min.js"",
        ""jquery.js""
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
            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.js")));

            int result = command.Execute("jquery@2.2.0");

            Assert.AreEqual(0, result);
            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.min.js")));
            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.js")));

            var logger = HostEnvironment.Logger as TestLogger;
            string message = "No library found with name \"jquery@2.2.0\" to update.\r\nPlease specify a library name without the version information to update.";

            Assert.AreEqual(StringHelper.NormalizeNewLines(message), StringHelper.NormalizeNewLines(logger.Messages.Last().Value));

            string actualText = File.ReadAllText(libmanjsonPath);

            Assert.AreEqual(StringHelper.NormalizeNewLines(contents), StringHelper.NormalizeNewLines(actualText));
        }

        [TestMethod]
        public void TestUpdateCommand_AlreadyUpToDate()
        {
            var command = new UpdateCommand(HostEnvironment);
            command.Configure(null);

            string contents = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""defaultDestination"": ""wwwroot"",
  ""libraries"": [
  ]
}";

            string libmanjsonPath = Path.Combine(WorkingDir, "libman.json");
            File.WriteAllText(libmanjsonPath, contents);

            var installCommand = new InstallCommand(HostEnvironment);
            installCommand.Configure(null);
            installCommand.Execute("jquery --files jquery.min.js --files jquery.js".Split(' '));

            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.min.js")));
            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.js")));

            int result = command.Execute("jquery");

            Assert.AreEqual(0, result);
            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.min.js")));
            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.js")));

            var logger = HostEnvironment.Logger as TestLogger;

            Assert.AreEqual("The library \"jquery\" is already up to date", logger.Messages.Last().Value);
        }

        [TestMethod]
        public void TestUpdateCommand_NotInstalled()
        {
            var command = new UpdateCommand(HostEnvironment);
            command.Configure(null);

            string contents = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""defaultDestination"": ""wwwroot"",
  ""libraries"": [
    {
      ""library"": ""jquery@3.3.1"",
      ""files"": [ ""jquery.min.js"", ""jquery.js"" ]
    }
  ]
}";

            string libmanjsonPath = Path.Combine(WorkingDir, "libman.json");
            File.WriteAllText(libmanjsonPath, contents);

            var restoreCommand = new RestoreCommand(HostEnvironment);
            restoreCommand.Configure(null);

            restoreCommand.Execute();
            _ = command.Execute("jqu");

            string actualText = File.ReadAllText(libmanjsonPath);

            Assert.AreEqual(StringHelper.NormalizeNewLines(contents), StringHelper.NormalizeNewLines(actualText));

            var logger = HostEnvironment.Logger as TestLogger;
            string message = "No library found with name \"jqu\" to update.\r\nPlease specify a library name without the version information to update.";

            Assert.AreEqual(StringHelper.NormalizeNewLines(message), StringHelper.NormalizeNewLines(logger.Messages.Last().Value));
        }

        [TestMethod]
        public void TestUpdateCommand_ToVersion()
        {
            var command = new UpdateCommand(HostEnvironment);
            command.Configure(null);

            string contents = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""defaultDestination"": ""wwwroot"",
  ""libraries"": [
    {
      ""library"": ""jquery@2.2.0"",
      ""files"": [ ""jquery.min.js"", ""jquery.js"" ]
    }
  ]
}";

            string libmanjsonPath = Path.Combine(WorkingDir, "libman.json");
            File.WriteAllText(libmanjsonPath, contents);

            var restoreCommand = new RestoreCommand(HostEnvironment);
            restoreCommand.Configure(null);

            restoreCommand.Execute();

            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.min.js")));
            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.js")));

            int result = command.Execute("jquery", "--to", "2.2.1");

            Assert.AreEqual(0, result);
            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.min.js")));
            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.js")));

            string actualText = File.ReadAllText(libmanjsonPath);

            string expectedText = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""defaultDestination"": ""wwwroot"",
  ""libraries"": [
    {
      ""library"": ""jquery@2.2.1"",
      ""files"": [
        ""jquery.min.js"",
        ""jquery.js""
      ]
    }
  ]
}";
            Assert.AreEqual(StringHelper.NormalizeNewLines(expectedText), StringHelper.NormalizeNewLines(actualText));
        }

        [TestMethod]
        public void TestUpdateCommand_InvalidUpdatedLibrary()
        {
            var command = new UpdateCommand(HostEnvironment);
            command.Configure(null);

            string contents = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""defaultDestination"": ""wwwroot"",
  ""libraries"": [
    {
      ""library"": ""jquery@2.2.0"",
      ""files"": [ ""jquery.min.js"", ""jquery.js"" ]
    }
  ]
}";

            string libmanjsonPath = Path.Combine(WorkingDir, "libman.json");
            File.WriteAllText(libmanjsonPath, contents);

            var restoreCommand = new RestoreCommand(HostEnvironment);
            restoreCommand.Configure(null);

            restoreCommand.Execute();

            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.min.js")));
            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.js")));

            int result = command.Execute("jquery", "--to", "twitter-bootstrap@4.0.0");

            Assert.IsFalse(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.min.js")));
            Assert.IsFalse(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.js")));

            string actualText = File.ReadAllText(libmanjsonPath);

            Assert.AreEqual(StringHelper.NormalizeNewLines(contents), StringHelper.NormalizeNewLines(actualText));

            var logger = HostEnvironment.Logger as TestLogger;

            Assert.AreEqual(LogLevel.Error, logger.Messages.Last().Key);

            string expectedMessage = "Failed to update \"jquery\" to \"twitter-bootstrap@4.0.0\"";

            Assert.IsTrue(logger.Messages.Any(m => m.Value == expectedMessage));
        }
    }
}
