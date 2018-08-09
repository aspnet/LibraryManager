// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Contracts
{
    /// <summary>
    /// Represents a catalog of libraries that can be searched by the <see cref="IProvider"/>.
    /// </summary>
    public interface ILibraryCatalog
    {
        /// <summary>
        /// Gets a list of completion spans for use in the JSON file.
        /// </summary>
        /// <param name="value">The current state of the library ID.</param>
        /// <param name="caretPosition">The caret position inside the <paramref name="value"/>.</param>
        Task<CompletionSet> GetLibraryCompletionSetAsync(string value, int caretPosition);

        /// <summary>
        /// Gets the library group from the specified <paramref name="libraryName"/>.
        /// </summary>
        /// <param name="libraryName">The name of the library.</param>
        /// <param name="version">The version of the library</param>
        /// <param name="cancellationToken">A token that allows the search to be cancelled.</param>
        /// <returns>An instance of <see cref="ILibraryGroup"/> or <code>null</code>.</returns>
        Task<ILibrary> GetLibraryAsync(string libraryName, string version, CancellationToken cancellationToken);

        /// <summary>
        /// Searches the catalog for the specified search term.
        /// </summary>
        /// <param name="term">The search term.</param>
        /// <param name="maxHits">The maximum number of results to return.</param>
        /// <param name="cancellationToken">A token that allows the search to be cancelled.</param>
        Task<IReadOnlyList<ILibraryGroup>> SearchAsync(string term, int maxHits, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the latest version of the library.
        /// </summary>
        /// <param name="libraryName">The library identifier.</param>
        /// <param name="includePreReleases">if set to <c>true</c> includes pre-releases.</param>
        /// <param name="cancellationToken">A token that allows the search to be cancelled.</param>
        /// <returns>The library identifier of the latest released version.</returns>
        Task<string> GetLatestVersion(string libraryName, bool includePreReleases, CancellationToken cancellationToken);
    }
}
