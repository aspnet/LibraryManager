using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Providers.Unpkg
{
    internal interface INpmPackageInfoCache
    {
        Task<NpmPackageInfo> GetPackageInfoAsync(string packageName, CancellationToken cancellationToken);
    }
}
