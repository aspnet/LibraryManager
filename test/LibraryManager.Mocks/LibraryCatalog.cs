// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryManager.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Mocks
{
    /// <summary>
    /// A mock of <see cref="ILibraryCatalog"/> for use in unit testing.
    /// </summary>
    /// <seealso cref="LibraryManager.Contracts.ILibraryCatalog" />
    public class LibraryCatalog : ILibraryCatalog
    {
        private readonly Dictionary<string, ILibrary> _libraries;
        private readonly Dictionary<string, SortedSet<ILibrary>> _librariesGroupedByName;

        /// <summary>
        /// Creates a mock Library Catalog
        /// </summary>
        public LibraryCatalog()
        {
            _libraries = new Dictionary<string, ILibrary>();
            _librariesGroupedByName = new Dictionary<string, SortedSet<ILibrary>>();
        }

        /// <summary>
        /// Gets the latest version of the library.
        /// </summary>
        /// <param name="libraryId">The library identifier.</param>
        /// <param name="includePreReleases">if set to <c>true</c> includes pre-releases.</param>
        /// <param name="cancellationToken">A token that allows the search to be cancelled.</param>
        /// <returns>
        /// The library identifier of the latest released version.
        /// </returns>
        public virtual Task<string> GetLatestVersion(string libraryId, bool includePreReleases, CancellationToken cancellationToken)
        {
            if (_librariesGroupedByName.ContainsKey(libraryId))
            {
                return Task.FromResult(_librariesGroupedByName[libraryId].Max.Version);
            }

            return Task.FromResult<string>(null);
        }

        /// <summary>
        /// Gets the library group from the specified <paramref name="libraryId" />.
        /// </summary>
        /// <param name="libraryId">Name of the library</param>
        /// <param name="version">Version of the library</param>
        /// <param name="cancellationToken">A token that allows the search to be cancelled.</param>
        /// <returns>
        /// An instance of <see cref="T:LibraryManager.Contracts.ILibraryGroup" /> or <code>null</code>.
        /// </returns>
        public virtual Task<ILibrary> GetLibraryAsync(string libraryId, string version, CancellationToken cancellationToken)
        {
            string id = libraryId + "@" + version;
            if (_libraries.ContainsKey(id))
            {
                return Task.FromResult(_libraries[id]);
            }

            return Task.FromResult<ILibrary>(null);
        }

        /// <summary>
        /// Gets a list of completion spans for use in the JSON file.
        /// </summary>
        /// <param name="value">The current state of the library ID.</param>
        /// <param name="caretPosition">The caret position inside the <paramref name="value" />.</param>
        public virtual Task<CompletionSet> GetLibraryCompletionSetAsync(string value, int caretPosition)
        {
            string searchTerm = value.Substring(0, caretPosition);
            IEnumerable<string> completions = _libraries.Keys.Where(k => k.StartsWith(searchTerm));
            var completionSet = new CompletionSet
            {
                Start = 0,
                Length = value.Length,
                Completions = completions.Select(c => new CompletionItem { DisplayText = c, InsertionText = c }),
            };

            return Task.FromResult(completionSet);
        }

        /// <summary>
        /// Searches the catalog for the specified search term.
        /// </summary>
        /// <param name="term">The search term.</param>
        /// <param name="maxHits">The maximum number of results to return.</param>
        /// <param name="cancellationToken">A token that allows the search to be cancelled.</param>
        public virtual Task<IReadOnlyList<ILibraryGroup>> SearchAsync(string term, int maxHits, CancellationToken cancellationToken)
        {
            IReadOnlyList<ILibraryGroup> list = _librariesGroupedByName.Keys
                                                  .Where(k => k.StartsWith(term))
                                                  .Take(maxHits)
                                                  .Select(k => new LibraryGroup() { DisplayName = k, Description = "Mock" })
                                                  .Cast<ILibraryGroup>()
                                                  .ToList();

            return Task.FromResult(list);
        }

        /// <summary>
        /// Add a library into the mock catalog
        /// </summary>
        /// <param name="library"></param>
        /// <returns></returns>
        public LibraryCatalog AddLibrary(ILibrary library)
        {
            _libraries.Add(library.Name + "@" + library.Version, library);

            if (!_librariesGroupedByName.ContainsKey(library.Name))
            {
                _librariesGroupedByName[library.Name] = new SortedSet<ILibrary>(Comparer<ILibrary>.Create((a, b) => string.Compare(a.Version, b.Version)));
            }

            _librariesGroupedByName[library.Name].Add(library);

            return this;
        }

    }
}
