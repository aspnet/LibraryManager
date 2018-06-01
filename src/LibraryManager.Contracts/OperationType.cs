// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
