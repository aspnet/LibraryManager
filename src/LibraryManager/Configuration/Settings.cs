// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Web.LibraryManager.Contracts.Configuration;
using Microsoft.Web.LibraryManager.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Web.LibraryManager.Configuration
{
    /// <summary>
    /// Real implementation of the configuration store for ISettings.
    /// </summary>
    public class Settings : ISettings
    {
        /// <summary>
        /// File name where user settings (not project settings) are stored
        /// </summary>
        private const string ConfigFileName = "libman.config.json";

        private JObject _rootObject;
        private JObject _configObject;

        /// <summary>
        /// Path where the configuration is persisted
        /// </summary>
        protected virtual string ConfigFilePath => Path.Combine(UserDataRoot, ConfigFileName);

        /// <summary>
        /// Default instance of the configuration settings
        /// </summary>
        public static Settings DefaultSettings { get; } = new Settings();

        /// <summary>
        /// Initialize a new Settings instance.
        /// </summary>
        /// <remarks>
        /// Modifying multiple settings instances will interfere with each other.  Consumers should
        /// use the ISettings instance exposed via the IHostInteraction interface.
        /// </remarks>
        protected Settings()
        {
            if (File.Exists(ConfigFilePath))
            {
                try
                {
                    using (var tr = new StreamReader(ConfigFilePath))
                    using (var jsonReader = new JsonTextReader(tr))
                    {
                        _rootObject = JObject.ReadFrom(jsonReader) as JObject;
                        if (_rootObject != null)
                        {
                            _configObject = _rootObject["config"] as JObject;
                            if (_configObject is null)
                            {
                                _configObject = new JObject();
                                _rootObject.Add("config", _configObject);
                            }
                        }
                    }
                }
                catch (JsonReaderException)
                {
                    // If there were any errors, we'll reset the file
                }
            }

            if (_rootObject is null)
            {
                InitSettingsFile(ConfigFilePath);
            }
        }

        private void InitSettingsFile(string configFilePath)
        {
            _rootObject = new JObject(new JProperty("config", new JObject()));
            _configObject = _rootObject["config"] as JObject;

            SaveSettingsFile(configFilePath, _rootObject);
        }

        /// <inheritdoc />
        public string UserDataRoot
        {
            get
            {
                string envVar = "%HOME%";

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    envVar = "%USERPROFILE%";
                }

                return Path.Combine(Environment.ExpandEnvironmentVariables(envVar), ".librarymanager");
            }
        }

        /// <inheritdoc />
        public bool TryGetValue(string settingName, out string value)
        {
            value = string.Empty;

            string envValue = Environment.GetEnvironmentVariable(settingName);
            if(!string.IsNullOrEmpty(envValue))
            {
                value = envValue;
                return true;
            }

            JToken setting = _configObject[settingName];
            if (setting?.Type == JTokenType.String)
            {
                value = setting.ToString();
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public bool TryGetEncryptedValue(string settingName, out string value)
        {
            value = string.Empty;
            bool fetch = TryGetValue(settingName, out string encryptedValue);

            if (fetch)
            {
                value = EncryptionUtility.DecryptString(encryptedValue);
            }

            return fetch;
        }

        /// <inheritdoc />
        public void SetValue(string settingName, string value)
        {
            if (_configObject[settingName] is null)
            {
                var valueProp = new JProperty(settingName, value);
                _configObject.Add(valueProp);
            }
            else
            {
                _configObject[settingName].Replace(value);
            }

            SaveSettingsFile(ConfigFilePath, _rootObject);
        }

        /// <inheritdoc />
        public void SetEncryptedValue(string settingName, string value)
        {
            string encryptedValue = EncryptionUtility.EncryptString(value);
            SetValue(settingName, encryptedValue);
        }

        /// <inheritdoc />
        public void RemoveValue(string settingName)
        {
            _configObject.Remove(settingName);
            SaveSettingsFile(ConfigFilePath, _rootObject);
        }

        private static void SaveSettingsFile(string filePath, JToken token)
        {
            // ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            using (var tw = new StreamWriter(filePath))
            {
                tw.Write(token.ToString());
            }
        }
    }
}
