// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Tools.Commands;

namespace Microsoft.Web.LibraryManager.Tools.Test
{
    [TestClass]
    public class LibmanInitTests : CommandTestBase
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
        public void TestInit()
        {
            InitCommand command = new InitCommand(HostEnvironment);

            command.Configure(null);

            int result = command.Execute("--default-destination", "wwwroot", "--default-provider", "cdnjs");

            Assert.AreEqual(0, result);

            string libmanFilePath = Path.Combine(WorkingDir, HostEnvironment.EnvironmentSettings.ManifestFileName);
            Assert.IsTrue(File.Exists(libmanFilePath));

            string contents = File.ReadAllText(libmanFilePath);

            string expectedContents = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""defaultDestination"": ""wwwroot"",
  ""libraries"": []
}";

            Assert.AreEqual(StringHelper.NormalizeNewLines(expectedContents), StringHelper.NormalizeNewLines(contents));
        }

        [TestMethod]
        public void TestInit_Interactive()
        {
            TestInputReader reader = HostEnvironment.InputReader as TestInputReader;

            reader.Inputs.Add("DefaultProvider", "cdnjs");
            reader.Inputs.Add("DefaultDestination:", "wwwroot");

            InitCommand command = new InitCommand(HostEnvironment);
            command.Configure(null);

            int result = command.Execute();

            Assert.AreEqual(0, result);

            string libmanFilePath = Path.Combine(WorkingDir, HostEnvironment.EnvironmentSettings.ManifestFileName);
            Assert.IsTrue(File.Exists(libmanFilePath));

            string contents = File.ReadAllText(libmanFilePath);

            string expectedContents = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""libraries"": []
}";

            Assert.AreEqual(StringHelper.NormalizeNewLines(expectedContents), StringHelper.NormalizeNewLines(contents));
        }

        [TestMethod]
        public void TestInit_UseDefault()
        {
            HostEnvironment.EnvironmentSettings.DefaultProvider = "unpkg";
            InitCommand command = new InitCommand(HostEnvironment);

            command.Configure(null);

            int result = command.Execute("-y");

            Assert.AreEqual(0, result);

            string libmanFilePath = Path.Combine(WorkingDir, HostEnvironment.EnvironmentSettings.ManifestFileName);
            Assert.IsTrue(File.Exists(libmanFilePath));

            string contents = File.ReadAllText(libmanFilePath);

            string expectedContents = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""unpkg"",
  ""libraries"": []
}";

            Assert.AreEqual(StringHelper.NormalizeNewLines(expectedContents), StringHelper.NormalizeNewLines(contents));
        }
    }
}
