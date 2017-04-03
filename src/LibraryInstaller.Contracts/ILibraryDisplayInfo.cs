// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace LibraryInstaller.Contracts
{
    /// <summary>
    /// Represents what is needed for an individual libary to be displayed.
    /// </summary>
    public interface ILibraryDisplayInfo
    {
        /// <summary>
        /// Gets the unique identifier of the library.
        /// </summary>
        string LibraryId { get; }

        /// <summary>
        /// The version of the the specific <see cref="ILibrary"/>.
        /// </summary>
        string Version { get; }
    }
}