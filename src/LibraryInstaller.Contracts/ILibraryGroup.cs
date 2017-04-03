// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryInstaller.Contracts
{
    /// <summary>
    /// Represents the search result for a specific library.
    /// </summary>
    public interface ILibraryGroup
    {
        /// <summary>
        /// The user facing display name of the library.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// The description of the library.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the <see cref="ILibraryDisplayInfo"/> objects for the different versions of the library.
        /// </summary>
        /// <param name="cancellationToken">A token that allows cancellation of the operation.</param>
        /// <returns>A list of <see cref="ILibraryDisplayInfo"/> used to display library information to the user.</returns>
        Task<IReadOnlyList<ILibraryDisplayInfo>> GetDisplayInfosAsync(CancellationToken cancellationToken);
    }
}