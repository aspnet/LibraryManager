// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Web.LibraryManager.Tools.Test
{
    [TestClass]
    public class BaseCommandTests : CommandTestBase
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
        public void TestGetManifest()
        {
            var command = new SampleTestCommand(HostEnvironment);
            command.Configure(null);
            command.CreateNewManifest = true;
            command.DefaultDestination = "wwwroot/lib";
            command.DefaultProvider = "cdnjs";
            command.Execute();

            Assert.AreEqual("1.0", command.Manifest.Version);

        }

        [TestMethod]
        public void TestGetManifestAsync_ThrowsIfNotCreatingNewOne()
        {
            var command = new SampleTestCommand(HostEnvironment);
            command.Configure(null);
            try
            {
                command.Execute();
            }
            catch (AggregateException age)
            {
                Assert.AreEqual(1, age.InnerExceptions.Count);
                Assert.AreEqual(typeof(InvalidOperationException), age.InnerExceptions[0].GetType());

                return;
            }

            Assert.Fail();

        }

        [TestMethod]
        public void TestGetManifestAsync_GetsExistingManifest()
        {
            var command = new SampleTestCommand(HostEnvironment);
            command.Configure(null);

            string content = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""defaultDestination"": ""wwwroot"",
  ""libraries"": []
}";

            string libmanFilePath = Path.Combine(WorkingDir, "libman.json");
            File.WriteAllText(libmanFilePath, content);

            command.Execute();

            Manifest manifest = command.Manifest;

            Assert.IsTrue(!manifest.Libraries.Any());
            Assert.AreEqual("wwwroot", manifest.DefaultDestination);
            Assert.AreEqual("cdnjs", manifest.DefaultProvider);
        }

        [TestMethod]
        public void TestGetManifest_FailsIfInvalidManifest()
        {
            var command = new SampleTestCommand(HostEnvironment);
            command.Configure(null);

            // Invalid Json content. No commas after fields.
            string content = @"{
  ""version"": ""1.0""
  ""defaultProvider"": ""cdnjs""
  ""defaultDestination"": ""wwwroot""
  ""libraries"": []
}";

            string libmanFilePath = Path.Combine(WorkingDir, "libman.json");
            File.WriteAllText(libmanFilePath, content);
            try
            {
                command.Execute();
            }
            catch (AggregateException age)
            {
                var ioe = age.InnerExceptions[0] as InvalidOperationException;
                if (ioe == null)
                {
                    Assert.Fail($"Unexpected exception thrown: {age.InnerExceptions[0].GetType().Name}");
                }

                Assert.AreEqual("Please fix the libman.json file and try again", ioe.Message);
            }

            Manifest manifest = command.Manifest;

            Assert.IsNull(manifest);
            var logger = HostEnvironment.Logger as TestLogger;

            Assert.AreEqual("Library Manager manifest contains syntax errors. Please fix the errors in libman.json, then try again.", logger.Messages[0].Value);
        }
    }
}
