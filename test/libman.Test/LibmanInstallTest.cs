// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Tools.Commands;

namespace Microsoft.Web.LibraryManager.Tools.Test
{
    [TestClass]
    public class LibmanInstallTest : CommandTestBase
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
        public void TestInstall_CleanDirectory()
        {
            var command = new InstallCommand(HostEnvironment);
            command.Configure(null);

            command.Execute("jquery@3.2.1", "--provider", "cdnjs", "--destination", "wwwroot");

            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "libman.json")));

            string text = File.ReadAllText(Path.Combine(WorkingDir, "libman.json"));
            string expectedText = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""libraries"": [
    {
      ""library"": ""jquery@3.2.1"",
      ""destination"": ""wwwroot""
    }
  ]
}";
            Assert.AreEqual(StringHelper.NormalizeNewLines(expectedText), StringHelper.NormalizeNewLines(text));
        }

        [TestMethod]
        public void TestInstall_CleanDirectory_WithPromptForProvider()
        {
            var testInputReader = HostEnvironment.InputReader as TestInputReader;

            testInputReader.Inputs.Add("DefaultProvider", "cdnjs");

            var command = new InstallCommand(HostEnvironment);
            command.Configure(null);

            command.Execute("jquery@3.2.1", "--destination", "wwwroot");

            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "libman.json")));

            string text = File.ReadAllText(Path.Combine(WorkingDir, "libman.json"));
            string expectedText = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""libraries"": [
    {
      ""library"": ""jquery@3.2.1"",
      ""destination"": ""wwwroot""
    }
  ]
}";
            Assert.AreEqual(StringHelper.NormalizeNewLines(expectedText), StringHelper.NormalizeNewLines(text));
        }

        [TestMethod]
        public void TestInstall_ExistingLibman_WithPromptForProvider()
        {
            var testInputReader = HostEnvironment.InputReader as TestInputReader;

            testInputReader.Inputs.Add("ProviderId", "cdnjs");

            string initialContent = @"{
  ""version"": ""1.0"",
  ""libraries"": [
  ]
}";

            File.WriteAllText(Path.Combine(WorkingDir, "libman.json"), initialContent);
            var command = new InstallCommand(HostEnvironment);
            command.Configure(null);

            command.Execute("jquery@3.2.1", "--destination", "wwwroot");

            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "libman.json")));

            string text = File.ReadAllText(Path.Combine(WorkingDir, "libman.json"));
            string expectedText = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""libraries"": [
    {
      ""library"": ""jquery@3.2.1"",
      ""destination"": ""wwwroot""
    }
  ]
}";
            Assert.AreEqual(StringHelper.NormalizeNewLines(expectedText), StringHelper.NormalizeNewLines(text));
        }


        [TestMethod]
        public void TestInstall_WithExistingLibmanJson()
        {
            var command = new InstallCommand(HostEnvironment);
            command.Configure(null);

            string initialContent = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""defaultDestination"": ""wwwroot"",
  ""libraries"": [
  ]
}";

            File.WriteAllText(Path.Combine(WorkingDir, "libman.json"), initialContent);

            command.Execute("jquery@3.2.1");

            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "libman.json")));

            string actualText = File.ReadAllText(Path.Combine(WorkingDir, "libman.json"));
            string expectedText = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""defaultDestination"": ""wwwroot"",
  ""libraries"": [
    {
      ""library"": ""jquery@3.2.1""
    }
  ]
}";
            Assert.AreEqual(StringHelper.NormalizeNewLines(expectedText), StringHelper.NormalizeNewLines(actualText));
        }

        [TestMethod]
        public void TestInstall_Duplicate()
        {
            var command = new InstallCommand(HostEnvironment);
            command.Configure(null);

            string initialContent = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""defaultDestination"": ""wwwroot"",
  ""libraries"": [
    {
      ""library"": ""jquery@3.2.1""
    }
  ]
}";

            File.WriteAllText(Path.Combine(WorkingDir, "libman.json"), initialContent);

            command.Execute("jquery@3.2.1", "--provider", "cdnjs");

            var testLogger = HostEnvironment.Logger as TestLogger;
            Assert.AreEqual("[LIB019]: Cannot restore. Multiple definitions for libraries: jquery", testLogger.Messages.Last().Value);
        }

        [TestMethod]
        public void TestInstall_WithExistingLibmanJson_SpecificFiles()
        {
            var command = new InstallCommand(HostEnvironment);
            command.Configure(null);

            string initialContent = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""defaultDestination"": ""wwwroot"",
  ""libraries"": [
  ]
}";

            File.WriteAllText(Path.Combine(WorkingDir, "libman.json"), initialContent);
            command.Execute("jquery@3.2.1", "--files", "jquery.min.js");

            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "libman.json")));

            string actualText = File.ReadAllText(Path.Combine(WorkingDir, "libman.json"));
            string expectedText = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""defaultDestination"": ""wwwroot"",
  ""libraries"": [
    {
      ""library"": ""jquery@3.2.1"",
      ""files"": [
        ""jquery.min.js""
      ]
    }
  ]
}";
            Assert.AreEqual(StringHelper.NormalizeNewLines(expectedText), StringHelper.NormalizeNewLines(actualText));
        }

        [TestMethod]
        public void TestInstall_WithInvalidFiles()
        {
            var command = new InstallCommand(HostEnvironment);
            command.Configure(null);

            string initialContent = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""defaultDestination"": ""wwwroot"",
  ""libraries"": [
  ]
}";

            File.WriteAllText(Path.Combine(WorkingDir, "libman.json"), initialContent);
            command.Execute("jquery@3.5.0", "--files", "abc.js");
            string expectedMessage = @"[LIB018]: ""jquery@3.5.0"" does not contain the following: abc.js
Valid files are jquery.js, jquery.min.js, jquery.min.map, jquery.slim.js, jquery.slim.min.js, jquery.slim.min.map";

            var logger = HostEnvironment.Logger as TestLogger;
            Assert.AreEqual(expectedMessage, logger.Messages.Last().Value);

            string actualText = File.ReadAllText(Path.Combine(WorkingDir, "libman.json"));
            Assert.AreEqual(StringHelper.NormalizeNewLines(initialContent), StringHelper.NormalizeNewLines(actualText));
        }
    }
}
