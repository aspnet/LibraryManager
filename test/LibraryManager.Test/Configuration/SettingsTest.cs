// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Configuration;
using Microsoft.Web.LibraryManager.Test.TestUtilities;

namespace Microsoft.Web.LibraryManager.Test.Configuration
{
    [TestClass]
    public class SettingsTest
    {
        private class TestSettings : Settings
        {
            private readonly string _configPath = SettingsTest.TestFilePath;

            public TestSettings(string configFilePath)
            {
                _configPath = configFilePath;
            }

            protected override string ConfigFilePath => _configPath;
        }

        private static string TestFilePath;

        [ClassInitialize]
        public static void Setup(TestContext context)
        {
#if NETFRAMEWORK
            TestFilePath = Path.Combine(context.DeploymentDirectory, "SettingsTest", "libman.config.json");
#else
            TestFilePath = Environment.ExpandEnvironmentVariables(@"%localappdata%\Microsoft\Library\libman.config.json");
#endif
            Directory.CreateDirectory(Path.GetDirectoryName(TestFilePath));
        }

        [ClassCleanup]
        public static void Cleanup()
        {
#if !NETFRAMEWORK
            // cleanup when we're leaving files behind not under the test DeploymentDirectory
            Directory.Delete(Path.GetDirectoryName(TestFilePath), true);
#endif
        }

        [TestMethod]
        public void Constructor_FileDoesNotExist()
        {
            if (File.Exists(TestFilePath))
            {
                File.Delete(TestFilePath);
            }

            var settings = new TestSettings(TestFilePath);

            // verify settings does not throw, does not have a well-known value
            Assert.IsFalse(settings.TryGetValue(Constants.HttpsProxy, out _));
        }

        [TestMethod]
        public void Constructor_FileExistsButEmpty()
        {
            File.WriteAllText(TestFilePath, "");

            var settings = new TestSettings(TestFilePath);

            // verify settings does not throw, does not have a well-known value
            Assert.IsFalse(settings.TryGetValue(Constants.HttpsProxy, out _));
        }

        [TestMethod]
        public void Constructor_FileExistsButNotJsonObject_Initialize()
        {
            string fileText = @"[]"; // valid JSON, but not an object
            File.WriteAllText(TestFilePath, fileText);

            var settings = new TestSettings(TestFilePath);

            // verify settings does not throw, does not have a well-known value
            Assert.IsFalse(settings.TryGetValue(Constants.HttpsProxy, out _));
        }

        [TestMethod]
        public void TryGetValue_ConfigSectionDoesNotExist_ReturnFalseWithEmptyString()
        {
            string fileText = @"{}";
            File.WriteAllText(TestFilePath, fileText);

            bool success = new TestSettings(TestFilePath).TryGetValue("testKey", out string value);

            Assert.IsFalse(success);
            Assert.AreEqual(string.Empty, value);
        }

        [TestMethod]
        public void TryGetValue_ValueDoesNotExist_ReturnFalseWithEmptyString()
        {
            string fileText = @"{ ""config"": { ""otherValue"": ""value"" } }";
            File.WriteAllText(TestFilePath, fileText);

            bool success = new TestSettings(TestFilePath).TryGetValue("testKey", out string value);

            Assert.IsFalse(success);
            Assert.AreEqual(string.Empty, value);
        }

        [TestMethod]
        public void TryGetValue_SettingsNameIsCaseSensitive()
        {
            string fileText = @"{ ""config"": { ""testKey"": ""testValue"" } }";
            File.WriteAllText(TestFilePath, fileText);

            bool success = new TestSettings(TestFilePath).TryGetValue("TestKey", out string value);

            Assert.IsFalse(success);
        }

        [TestMethod]
        public void TryGetValue_IfSettingPresentAmongEnvironmentVariables_UseEnvironmentValue()
        {
            string fileText = @"{ ""config"": { ""testKey"": ""fileValue"" } }";
            File.WriteAllText(TestFilePath, fileText);

            Environment.SetEnvironmentVariable("testKey", "envValue");

            bool success = new TestSettings(TestFilePath).TryGetValue("testKey", out string value);

            // cleanup before we assert
            Environment.SetEnvironmentVariable("testKey", null);

            Assert.IsTrue(success);
            Assert.AreEqual("envValue", value);
        }

        [TestMethod]
        public void SetValue_ConfigSectionDoesNotExist_CreateSectionWithValue()
        {
            File.WriteAllText(TestFilePath, "");

            new TestSettings(TestFilePath).SetValue("testKey", "testValue");

            string contents = File.ReadAllText(TestFilePath);
            string expected = @"{
  ""config"": {
    ""testKey"": ""testValue""
  }
}";
            Assert.AreEqual(StringUtility.NormalizeNewLines(expected), StringUtility.NormalizeNewLines(contents));
        }

        [TestMethod]
        public void SetValue_ValueDoesNotExist_InsertValue()
        {
            string fileText = @"{ ""config"": { ""otherKey"": ""other"" } }";
            File.WriteAllText(TestFilePath, fileText);

            new TestSettings(TestFilePath).SetValue("testKey", "testValue");

            string contents = File.ReadAllText(TestFilePath);
            string expected = @"{
  ""config"": {
    ""otherKey"": ""other"",
    ""testKey"": ""testValue""
  }
}";
            Assert.AreEqual(StringUtility.NormalizeNewLines(expected), StringUtility.NormalizeNewLines(contents));
        }

        [TestMethod]
        public void SetValue_ValueExists_UpdateValue()
        {
            string fileText = @"{ ""config"": { ""testKey"": ""before"" } }";
            File.WriteAllText(TestFilePath, fileText);

            new TestSettings(TestFilePath).SetValue("testKey", "after");

            string contents = File.ReadAllText(TestFilePath);
            string expected = @"{
  ""config"": {
    ""testKey"": ""after""
  }
}";
            Assert.AreEqual(StringUtility.NormalizeNewLines(expected), StringUtility.NormalizeNewLines(contents));
        }

        [TestMethod]
        public void SetEncryptedValue_ValueIsDifferentFromOriginal()
        {
            var ut = new TestSettings(TestFilePath);

            ut.SetEncryptedValue("testKey", "testValue");

            // Verify that the key exists, but the value should be different than what we passed in.
            bool success = ut.TryGetValue("testKey", out string result);

            Assert.IsTrue(success);
            Assert.AreNotEqual("testValue", result);

            // Verify the value can be retrieved correctly
            success = ut.TryGetEncryptedValue("testKey", out result);

            Assert.IsTrue(success);
            Assert.AreEqual("testValue", result);
        }

        [TestMethod]
        public void RemoveValue_Existing()
        {
            var ut = new TestSettings(TestFilePath);
            ut.SetValue("testKey", "testValue");

            ut.RemoveValue("testKey");

            Assert.IsFalse(ut.TryGetValue("testKey", out _));
        }

        [TestMethod]
        public void RemoveValue_NonExisting()
        {
            var ut = new TestSettings(TestFilePath);

            ut.RemoveValue("testKey");

            Assert.IsFalse(ut.TryGetValue("testKey", out _));
        }

        [TestMethod]
        public void SetValue_AddingNewValue_DoesNotAffectOtherSections()
        {
            string fileText = @"{ ""foo"": {} }";
            File.WriteAllText(TestFilePath, fileText);

            new TestSettings(TestFilePath).SetValue("testKey", "testValue");

            string contents = File.ReadAllText(TestFilePath);
            string expected = @"{
  ""foo"": {},
  ""config"": {
    ""testKey"": ""testValue""
  }
}";
            Assert.AreEqual(StringUtility.NormalizeNewLines(expected), StringUtility.NormalizeNewLines(contents));
        }

    }
}
