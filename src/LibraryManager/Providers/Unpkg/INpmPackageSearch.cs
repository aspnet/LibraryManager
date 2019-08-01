using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Providers.Unpkg
{
    internal interface INpmPackageSearch
    {
        Task<IEnumerable<string>> GetPackageNamesAsync(string searchTerm, CancellationToken cancellationToken);

        Task<NpmPackageInfo> GetPackageInfoAsync(string packageName, CancellationToken cancellationToken);
    }
}
