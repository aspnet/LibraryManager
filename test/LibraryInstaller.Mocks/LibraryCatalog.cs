using LibraryInstaller.Contracts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryInstaller.Mocks
{
    /// <summary>
    /// A mock of <see cref="ILibraryCatalog"/> for use in unit testing.
    /// </summary>
    /// <seealso cref="LibraryInstaller.Contracts.ILibraryCatalog" />
    public class LibraryCatalog : ILibraryCatalog
    {
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
            return Task.FromResult(libraryId);
        }

        /// <summary>
        /// Gets the library group from the specified <paramref name="libraryId" />.
        /// </summary>
        /// <param name="libraryId">The unique library identifier.</param>
        /// <param name="cancellationToken">A token that allows the search to be cancelled.</param>
        /// <returns>
        /// An instance of <see cref="T:LibraryInstaller.Contracts.ILibraryGroup" /> or <code>null</code>.
        /// </returns>
        public virtual Task<ILibrary> GetLibraryAsync(string libraryId, CancellationToken cancellationToken)
        {
            var library = new Library
            {
                Name = "test",
                ProviderId = "test",
                Version = "1.0",
                Files = new Dictionary<string, bool> {
                    { "test.js", true }
                }
            };

            return Task.FromResult<ILibrary>(library);
        }

        /// <summary>
        /// Gets a list of completion spans for use in the JSON file.
        /// </summary>
        /// <param name="value">The current state of the library ID.</param>
        /// <param name="caretPosition">The caret position inside the <paramref name="value" />.</param>
        public Task<CompletionSet> GetLibraryCompletionSetAsync(string value, int caretPosition)
        {
            var completion = new CompletionSet
            {
                Start = 0,
                Length = value.Length,
                Completions = new List<CompletionItem>()
            };

            return Task.FromResult(completion);
        }

        /// <summary>
        /// Searches the catalog for the specified search term.
        /// </summary>
        /// <param name="term">The search term.</param>
        /// <param name="maxHits">The maximum number of results to return.</param>
        /// <param name="cancellationToken">A token that allows the search to be cancelled.</param>
        public Task<IReadOnlyList<ILibraryGroup>> SearchAsync(string term, int maxHits, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ILibraryGroup>>(null);
        }
    }
}
