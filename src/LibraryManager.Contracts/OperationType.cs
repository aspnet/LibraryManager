// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Web.LibraryManager.Contracts
{
    /// <summary>
    /// The Library Manager types of operation
    /// </summary>
    public enum OperationType
    {
        /// <summary>
        /// Restores a library
        /// </summary>
        Restore,

        /// <summary>
        /// Installs a library
        /// </summary>
        Install,

        /// <summary>
        /// Uninstalls a library
        /// </summary>
        Uninstall,

        /// <summary>
        /// Upgrades a library
        /// </summary>
        Upgrade,

        /// <summary>
        /// Cleans libraries
        /// </summary>
        Clean
    }
}
