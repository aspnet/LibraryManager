// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using LibraryInstaller.Contracts;
using System.Collections.Generic;

namespace LibraryInstaller.Mocks
{
    /// <summary>
    /// A mock <see cref="ILibraryInstallationState"/> class.
    /// </summary>
    /// <seealso cref="LibraryInstaller.Contracts.ILibraryInstallationState" />
    public class LibraryInstallationState : ILibraryInstallationState
    {
        /// <summary>
        /// The identifyer to uniquely identify the library
        /// </summary>
        public string LibraryId { get; set; }

        /// <summary>
        /// The unique identifier of the provider.
        /// </summary>
        public string ProviderId { get; set; }

        /// <summary>
        /// The list of file names to install
        /// </summary>
        public IReadOnlyList<string> Files { get; set; }

        /// <summary>
        /// The path relative to the working directory to copy the files to.
        /// </summary>
        public string Path { get; set; }
    }
}
