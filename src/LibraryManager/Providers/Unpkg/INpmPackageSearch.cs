using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Providers.Unpkg
{
    /// <summary>
    /// A utility to help retrieve all related package names from a given search term
    /// </summary>
    public interface INpmPackageSearch
    {
        /// <summary>
        /// Retrieve all related package names given a search term
        /// </summary>
        /// <param name="searchTerm"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IEnumerable<string>> GetPackageNamesAsync(string searchTerm, CancellationToken cancellationToken);
    }
}
