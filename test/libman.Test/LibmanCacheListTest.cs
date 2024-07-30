// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Tools.Commands;

namespace Microsoft.Web.LibraryManager.Tools.Test
{
    [TestClass]
    public class LibmanCacheListTest : CommandTestBase
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
        public void TestCacheList()
        {
            Directory.CreateDirectory(Path.Combine(CacheDir, "cdnjs", "jquery", "3.2.1"));
            Directory.CreateDirectory(Path.Combine(CacheDir, "cdnjs", "jquery", "2.2.0"));

            File.Create(Path.Combine(CacheDir, "cdnjs", "jquery", "3.2.1", "jquery.min.js"));
            File.Create(Path.Combine(CacheDir, "cdnjs", "jquery", "3.2.1", "core.js"));
            File.Create(Path.Combine(CacheDir, "cdnjs", "jquery", "3.2.1", "jquery.min.map"));

            File.Create(Path.Combine(CacheDir, "cdnjs", "jquery", "2.2.0", "jquery.min.js"));

            var cacheListCommand = new CacheListCommand(HostEnvironment);
            cacheListCommand.Configure(null);

            int result = cacheListCommand.Execute();
            Assert.AreEqual(0, result);

            var logger = HostEnvironment.Logger as TestLogger;

            string expectedString = $@"Cache root directory:
---------------------
{CacheDir}

Cache contents:
---------------
unpkg:
    (empty)
jsdelivr:
    (empty)
filesystem:
    (empty)
cdnjs:
    jquery
";
            Assert.AreEqual(StringHelper.NormalizeNewLines(expectedString), StringHelper.NormalizeNewLines(logger.Messages[0].Value));
        }

        [TestMethod]
        public void TestCacheList_Detailed()
        {
            Directory.CreateDirectory(Path.Combine(CacheDir, "cdnjs", "jquery", "3.2.1"));
            Directory.CreateDirectory(Path.Combine(CacheDir, "cdnjs", "jquery", "2.2.0"));

            File.Create(Path.Combine(CacheDir, "cdnjs", "jquery", "3.2.1", "jquery.min.js"));
            File.Create(Path.Combine(CacheDir, "cdnjs", "jquery", "3.2.1", "core.js"));
            File.Create(Path.Combine(CacheDir, "cdnjs", "jquery", "3.2.1", "jquery.min.map"));

            File.Create(Path.Combine(CacheDir, "cdnjs", "jquery", "2.2.0", "jquery.min.js"));

            var cacheListCommand = new CacheListCommand(HostEnvironment);
            cacheListCommand.Configure(null);

            int result = cacheListCommand.Execute("--files");
            Assert.AreEqual(0, result);

            var logger = HostEnvironment.Logger as TestLogger;

            var expectedString = $@"Cache root directory:
---------------------
{CacheDir}

Cache contents:
---------------
unpkg:
    (empty)
jsdelivr:
    (empty)
filesystem:
    (empty)
cdnjs:
    jquery
        2.2.0\jquery.min.js
        3.2.1\core.js
        3.2.1\jquery.min.js
        3.2.1\jquery.min.map
";
            Assert.AreEqual(StringHelper.NormalizeNewLines(expectedString), StringHelper.NormalizeNewLines(logger.Messages[0].Value));
        }
    }
}
