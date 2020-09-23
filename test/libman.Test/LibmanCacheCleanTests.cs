// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Tools.Commands;

namespace Microsoft.Web.LibraryManager.Tools.Test
{
    [TestClass]
    public class LibmanCacheCleanTests : CommandTestBase
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
        public void TestCacheClean()
        {
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

            var restoreCommand = new RestoreCommand(HostEnvironment);
            restoreCommand.Configure(null).Execute();

            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.min.js")));
            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "core.js")));

            Assert.IsTrue(File.Exists(Path.Combine(CacheDir, "cdnjs", "jquery", "3.2.1", "jquery.min.js")));
            Assert.IsTrue(File.Exists(Path.Combine(CacheDir, "cdnjs", "jquery", "3.2.1", "core.js")));

            var cleanCommand = new CacheCleanCommand(HostEnvironment);
            cleanCommand.Configure();

            cleanCommand.Execute();

            // Should not delete files in the project.
            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.min.js")));
            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "core.js")));

            // Should delete files in the cache.
            Assert.IsFalse(File.Exists(Path.Combine(CacheDir, "cdnjs", "jquery", "3.2.1", "jquery.min.js")));
            Assert.IsFalse(File.Exists(Path.Combine(CacheDir, "cdnjs", "jquery", "3.2.1", "core.js")));
        }

        [TestMethod]
        public void TestCacheClean_ForProvider()
        {
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

            Directory.CreateDirectory(Path.Combine(CacheDir, "filesystem"));
            File.WriteAllText(Path.Combine(CacheDir, "filesystem", "abc.js"), "");

            var restoreCommand = new RestoreCommand(HostEnvironment);
            restoreCommand.Configure(null).Execute();

            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.min.js")));
            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "core.js")));

            Assert.IsTrue(File.Exists(Path.Combine(CacheDir, "cdnjs", "jquery", "3.2.1", "jquery.min.js")));
            Assert.IsTrue(File.Exists(Path.Combine(CacheDir, "cdnjs", "jquery", "3.2.1", "core.js")));

            var cleanCommand = new CacheCleanCommand(HostEnvironment);
            cleanCommand.Configure();

            cleanCommand.Execute("cdnjs");

            // Should not delete files in the project.
            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "jquery.min.js")));
            Assert.IsTrue(File.Exists(Path.Combine(WorkingDir, "wwwroot", "core.js")));

            // Should delete files in the cache.
            Assert.IsFalse(File.Exists(Path.Combine(CacheDir, "cdnjs", "jquery", "3.2.1", "jquery.min.js")));
            Assert.IsFalse(File.Exists(Path.Combine(CacheDir, "cdnjs", "jquery", "3.2.1", "core.js")));

            Assert.IsTrue(File.Exists(Path.Combine(CacheDir, "filesystem", "abc.js")));
        }

        [TestMethod]
        public void TestCacheClean_ThrowsIfUnknownProvider()
        {
            var cleanCommand = new CacheCleanCommand(HostEnvironment);
            cleanCommand.Configure();

            var exception = Assert.ThrowsException<AggregateException>(() => cleanCommand.Execute("foo"));
            Assert.IsInstanceOfType(exception.InnerExceptions.First(), typeof(InvalidOperationException));
        }
    }
}
