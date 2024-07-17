// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Contracts
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
        /// Gets a list of versions of the library.
        /// </summary>
        /// <param name="cancellationToken">A token that allows cancellation of the operation.</param>
        /// <returns>A list of library IDs used to display library information to the user.</returns>
        Task<IEnumerable<string>> GetLibraryVersions(CancellationToken cancellationToken);
    }
}
