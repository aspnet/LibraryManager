// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        /// The identifyer to uniquely identify the library
        /// </summary>
        public virtual string LibraryId { get; set; }

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
    }
}
