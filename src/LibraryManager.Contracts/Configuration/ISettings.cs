// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Web.LibraryManager.Contracts.Configuration
{
    /// <summary>
    /// Inteface for reading or writing configuration settings
    /// </summary>
    public interface ISettings
    {
        /// <summary>
        /// Root path where data for the current user is stored on disk 
        /// </summary>
        string UserDataRoot { get; }

        /// <summary>
        /// Attempts to fetch a saved configuration value
        /// </summary>
        /// <param name="settingName">Name of the setting to fetch</param>
        /// <param name="value">The fetched value, if successful; otherwise an empty string</param>
        /// <returns>True if the value was fetched successfully</returns>
        bool TryGetValue(string settingName, out string value);

        /// <summary>
        /// Attempts to fetch and decrypt a saved configuration value
        /// </summary>
        /// <param name="settingName">Name of the setting to fetch</param>
        /// <param name="value">The decrypted value, if successful; otherwise an empty string</param>
        /// <returns>True if the value was fetched and decrypted successfully</returns>
        bool TryGetEncryptedValue(string settingName, out string value);

        /// <summary>
        /// Add or update the specified setting with the specified value
        /// </summary>
        /// <param name="settingName">Name of the setting to be changed</param>
        /// <param name="value">Value to be saved</param>
        void SetValue(string settingName, string value);

        /// <summary>
        /// Add or update the specified setting with the specified value, encrypted for the current user.
        /// </summary>
        /// <param name="settingName">Name of the setting to be changed</param>
        /// <param name="value">Value to be encrypted and saved</param>
        void SetEncryptedValue(string settingName, string value);

        /// <summary>
        /// Removes the setting from the configuration
        /// </summary>
        /// <param name="settingName">Setting to remove</param>
        void RemoveValue(string settingName);
    }
}
