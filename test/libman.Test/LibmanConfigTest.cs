// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Tools.Commands;
using Moq;

namespace Microsoft.Web.LibraryManager.Tools.Test
{
    [TestClass]
    public class LibmanConfigTest : CommandTestBase
    {
        [TestInitialize]
        public override void Setup()
        {
            base.Setup();

            HostEnvironment.HostInteraction = new Mocks.HostInteractionInternal(string.Empty, string.Empty)
            {
                Logger = new LibraryManager.Mocks.Logger(),
                Settings = new LibraryManager.Mocks.Settings(),
            };
        }

        [TestCleanup]
        public override void Cleanup()
        {
            base.Cleanup();
        }

        [TestMethod]
        public void ReadNonExistingSetting_ShouldLogError()
        {
            var command = new ConfigCommand(HostEnvironment);
            command.Configure(null);

            try
            {
                _ = command.Execute("TestSetting");
                Assert.Fail();
            }
            catch(AggregateException ae)
            {
                var ioe = ae.InnerException as InvalidOperationException;
                Assert.IsNotNull(ioe);
                string expected = string.Format(Resources.Text.ConfigCommand_Error_KeyNotFound, "TestSetting");
                Assert.AreEqual(expected, ioe.Message);
            }
        }

        [TestMethod]
        public void ReadExistingSetting_ShouldPrintValueOnly()
        {
            HostEnvironment.HostInteraction.Settings.SetValue("TestSetting", "testValue");
            var mockLogger = new Mock<ILogger>();
            mockLogger.Setup(log => log.Log(It.Is<string>(s => s.Equals("testValue", StringComparison.Ordinal)),
                                        It.Is<LogLevel>(l => l == LogLevel.Task)));
            HostEnvironment.Logger = mockLogger.Object;

            var command = new ConfigCommand(HostEnvironment);
            command.Configure(null);

            _ = command.Execute("TestSetting");

            mockLogger.Verify();
        }

        [TestMethod]
        public void SetNewValue()
        {
            Assert.IsFalse(HostEnvironment.HostInteraction.Settings.TryGetValue("TestSetting", out _));
            var command = new ConfigCommand(HostEnvironment);
            command.Configure(null);

            _ = command.Execute("--set TestSetting=testValue".Split(" "));

            Assert.IsTrue(HostEnvironment.HostInteraction.Settings.TryGetValue("TestSetting", out string value));
            Assert.AreEqual("testValue", value);
        }

        [TestMethod]
        public void SetExistingValue()
        {
            HostEnvironment.HostInteraction.Settings.SetValue("TestSetting", "oldValue");
            var command = new ConfigCommand(HostEnvironment);
            command.Configure(null);

            _ = command.Execute("--set TestSetting=testValue".Split(" "));

            Assert.IsTrue(HostEnvironment.HostInteraction.Settings.TryGetValue("TestSetting", out string value));
            Assert.AreEqual("testValue", value);
        }

        [TestMethod]
        public void SetNewEncryptedValue()
        {
            Assert.IsFalse(HostEnvironment.HostInteraction.Settings.TryGetValue("TestSetting", out _));
            var command = new ConfigCommand(HostEnvironment);
            command.Configure(null);

            _ = command.Execute("--setEncrypted TestSetting=testValue".Split(" "));

            Assert.IsTrue(HostEnvironment.HostInteraction.Settings.TryGetValue("TestSetting", out string value));
            Assert.AreNotEqual("testValue", value);
        }

        [TestMethod]
        public void RemoveExistingValue()
        {
            HostEnvironment.HostInteraction.Settings.SetValue("TestSetting", "oldValue");
            var command = new ConfigCommand(HostEnvironment);
            command.Configure(null);

            _ = command.Execute("--set TestSetting=".Split(" "));

            Assert.IsFalse(HostEnvironment.HostInteraction.Settings.TryGetValue("TestSetting", out _));
        }

        [TestMethod]
        public void RemoveExistingEncryptedValue()
        {
            HostEnvironment.HostInteraction.Settings.SetEncryptedValue("TestSetting", "oldValue");
            var command = new ConfigCommand(HostEnvironment);
            command.Configure(null);

            _ = command.Execute("--setEncrypted TestSetting=".Split(" "));

            Assert.IsFalse(HostEnvironment.HostInteraction.Settings.TryGetValue("TestSetting", out _));
        }

        [TestMethod]
        public void ReadAndWriteInSameInvocation_ShouldFailWithError()
        {
            var command = new ConfigCommand(HostEnvironment);
            command.Configure(null);

            try
            {
                _ = command.Execute("TestSetting --set TestSetting2=testValue".Split(" "));
                Assert.Fail();
            }
            catch (AggregateException ae)
            {
                var ioe = ae.InnerException as InvalidOperationException;
                Assert.IsNotNull(ioe);
                string expected = Resources.Text.ConfigCommand_Error_ConflictingParameters;
                Assert.AreEqual(expected, ioe.Message);
            }
        }
    }
}
