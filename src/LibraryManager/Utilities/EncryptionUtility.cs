// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
