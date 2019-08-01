using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Providers.Unpkg
{
    internal sealed class NpmPackageInfoCache : INpmPackageInfoCache
    {
        private readonly Dictionary<string, NpmPackageInfo> _cachedPackages = new Dictionary<string, NpmPackageInfo>();
        private readonly INpmPackageSearch _npmPackageSearch;

        public NpmPackageInfoCache(INpmPackageSearch npmPackageSearch)
        {
            _npmPackageSearch = npmPackageSearch;
        }

        public async Task<NpmPackageInfo> GetPackageInfoAsync(string packageName, CancellationToken cancellationToken)
        {
            if (!_cachedPackages.TryGetValue(packageName, out NpmPackageInfo packageInfo))
            {
                packageInfo = await _npmPackageSearch.GetPackageInfoAsync(packageName, cancellationToken).ConfigureAwait(false);

                if (packageInfo != null)
                {
                    _cachedPackages[packageName] = packageInfo;
                }
            }

            return packageInfo;
        }
    }
}
