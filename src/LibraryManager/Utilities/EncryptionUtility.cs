// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Web.LibraryManager.Utilities
{
    /// <summary>
    /// Utility class for encrypting/decrypting data for the current user.
    /// </summary>
    internal static class EncryptionUtility
    {
        private static readonly byte[] EntropyBytes = Encoding.UTF8.GetBytes("LibraryManager");

        public static string EncryptString(string value)
        {
            byte[] decryptedByteArray = Encoding.UTF8.GetBytes(value);
            byte[] encryptedByteArray = ProtectedData.Protect(decryptedByteArray, EntropyBytes, DataProtectionScope.CurrentUser);
            string encryptedString = Convert.ToBase64String(encryptedByteArray);
            return encryptedString;
        }

        public static string DecryptString(string encryptedString)
        {
            byte[] encryptedByteArray = Convert.FromBase64String(encryptedString);
            byte[] decryptedByteArray = ProtectedData.Unprotect(encryptedByteArray, EntropyBytes, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedByteArray);
        }
    }
}
