using System.Collections.Generic;
using System.Linq;
using Microsoft.Web.LibraryManager.Contracts.Configuration;

namespace Microsoft.Web.LibraryManager.Mocks
{
    /// <inheritdoc/>
    public class Settings : ISettings
    {
        /// <summary>
        /// Create a mock Settings with no user data folder specified.
        /// </summary>
        public Settings()
        {
        }

        /// <summary>
        /// Create a mock settings with a specified path to the user data folder
        /// </summary>
        /// <param name="dataRoot"></param>
        public Settings(string dataRoot)
        {
            UserDataRoot = dataRoot;
        }

        readonly Dictionary<string, string> _settingsStore = new Dictionary<string, string>();

        private static string Reverse(string s)
        {
            return new string(s.Reverse().ToArray());
        }

        /// <inheritdoc/>
        public string UserDataRoot { get; set; }

        /// <inheritdoc/>
        public void SetEncryptedValue(string settingName, string value)
        {
            _settingsStore[settingName] = Reverse(value);
        }

        /// <inheritdoc/>
        public void SetValue(string settingName, string value)
        {
            _settingsStore[settingName] = value;
        }

        /// <inheritdoc/>
        public bool TryGetEncryptedValue(string settingName, out string value)
        {
            if (_settingsStore.ContainsKey(settingName))
            {
                value = Reverse(_settingsStore[settingName]);
                return true;
            }

            value = "";
            return false;
        }

        /// <inheritdoc/>
        public bool TryGetValue(string settingName, out string value)
        {
            if (_settingsStore.ContainsKey(settingName))
            {
                value = _settingsStore[settingName];
                return true;
            }

            value = "";
            return false;
        }

        /// <inheritdoc />
        public void RemoveValue(string settingName)
        {
            if (_settingsStore.ContainsKey(settingName))
            {
                _settingsStore.Remove(settingName);
            }
        }
    }
}
