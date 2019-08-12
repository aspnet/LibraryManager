using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Providers.Unpkg
{
    /// <summary>
    /// A factory for NPM package info
    /// </summary>
    public interface INpmPackageInfoFactory
    {
        /// <summary>
        /// Gets NpmPackageInfo for the given package
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>The NpmPackageInfo for the given packageName</returns>
        Task<NpmPackageInfo> GetPackageInfoAsync(string packageName, CancellationToken cancellationToken);
    }
}
