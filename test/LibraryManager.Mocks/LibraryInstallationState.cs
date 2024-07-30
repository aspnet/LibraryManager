// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Web.LibraryManager.Contracts;
using System.Collections.Generic;

namespace Microsoft.Web.LibraryManager.Mocks
{
    /// <summary>
    /// A mock <see cref="ILibraryInstallationState"/> class.
    /// </summary>
    /// <seealso cref="LibraryManager.Contracts.ILibraryInstallationState" />
    public class LibraryInstallationState : ILibraryInstallationState
    {
        /// <summary>
        /// The unique identifier of the provider.
        /// </summary>
        public virtual string ProviderId { get; set; }

        /// <summary>
        /// The list of file names to install
        /// </summary>
        public virtual IReadOnlyList<string> Files { get; set; }

        /// <summary>
        /// The path relative to the working directory to copy the files to.
        /// </summary>
        public virtual string DestinationPath { get; set; }

        /// <summary>
        /// Name of the library.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Version of the library.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Indicates whether the library is using the default destination
        /// </summary>
        public bool IsUsingDefaultDestination { get; set; }

        /// <summary>
        /// Indicates whether the library is using the default provider.
        /// </summary>
        public bool IsUsingDefaultProvider { get; set; }
    }
}
