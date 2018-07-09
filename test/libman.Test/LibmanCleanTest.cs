// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Tools.Commands;

namespace Microsoft.Web.LibraryManager.Tools.Test
{
    [TestClass]
    public class LibmanCleanTest : CommandTestBase
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
        public void TestClean()
        {
            string contents = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""defaultDestination"": ""wwwroot/lib/"",
  ""libraries"": [
    {
      ""library"": ""jquery@3.2.1"",
      ""files"": [ ""jquery.min.js"", ""core.js"" ]
    }
  ]
}";

            File.WriteAllText(Path.Combine(WorkingDir, "libman.json"), contents);

            var restoreCommand = new RestoreCommand(HostEnvironment);
            restoreCommand.Configure(null).Execute();

            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "lib", "jquery.min.js")));
            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "lib", "core.js")));

            var command = new CleanCommand(HostEnvironment);
            command.Configure(null);

            int result = command.Execute();
            Assert.AreEqual(0, result);

            Assert.IsFalse(File.Exists(Path.Combine(WorkingDir, "wwwroot", "lib", "jquery.min.js")));
            Assert.IsFalse(File.Exists(Path.Combine(WorkingDir, "wwwroot", "lib", "core.js")));
            Assert.IsFalse(Directory.Exists(Path.Combine(WorkingDir, "wwwroot", "lib")));
            Assert.IsFalse(Directory.Exists(Path.Combine(WorkingDir, "wwwroot")));
        }
    }
}
