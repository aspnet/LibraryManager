using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Providers.Unpkg
{
    /// <summary>
    /// A utility to help retrieve the info for all related packages from a given search term
    /// </summary>
    public interface INpmPackageSearch
    {
        /// <summary>
        /// Retrieves info for all related packages given a search term
        /// </summary>
        /// <param name="searchTerm"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IEnumerable<NpmPackageInfo>> GetPackageNamesAsync(string searchTerm, CancellationToken cancellationToken);
    }
}
