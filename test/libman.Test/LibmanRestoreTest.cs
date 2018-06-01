// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Tools.Commands;

namespace Microsoft.Web.LibraryManager.Tools.Test
{
    [TestClass]
    public class LibmanRestoreTest : CommandTestBase
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
        public void TestRestore()
        {
            var command = new RestoreCommand(HostEnvironment);
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

            File.WriteAllText(Path.Combine(WorkingDir, "libman.json"), contents);

            int result = command.Execute();
            Assert.AreEqual(0, result);

            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.min.js")));
            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "core.js")));
        }

        [TestMethod]
        public void TestRestoreErrors()
        {
            var command = new RestoreCommand(HostEnvironment);
            command.Configure(null);

            string contents = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""defaultDestination"": ""wwwroot"",
  ""libraries"": [
    {
      ""library"": ""jquery@3.2.1"",
      ""files"": [ ""jquery.min.js"", ""core123.js"" ]
    }
  ]
}";

            File.WriteAllText(Path.Combine(WorkingDir, "libman.json"), contents);

            int result = command.Execute();
            Assert.AreEqual(0, result);

            Assert.IsFalse(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.min.js")));
            Assert.IsFalse(File.Exists(Path.Combine(WorkingDir, "wwwroot", "core.js")));

            var logger = HostEnvironment.Logger as TestLogger;

            Assert.IsTrue(logger.Messages.Any(m => m.Key == LibraryManager.Contracts.LogLevel.Error));
        }
    }
}
