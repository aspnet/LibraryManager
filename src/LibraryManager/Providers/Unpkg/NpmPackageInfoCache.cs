using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Providers.Unpkg
{
    internal static class NpmPackageInfoCache
    {
        private static readonly Dictionary<string, NpmPackageInfo> CachedPackages = new Dictionary<string, NpmPackageInfo>();

        internal static async Task<NpmPackageInfo> GetPackageInfoAsync(string packageName, CancellationToken cancellationToken)
        {
            NpmPackageInfo packageInfo = null;

            if (!CachedPackages.TryGetValue(packageName, out packageInfo))
            {
                packageInfo = await NpmPackageSearch.GetPackageInfoAsync(packageName, cancellationToken).ConfigureAwait(false);

                if (packageInfo != null)
                {
                    CachedPackages[packageName] = packageInfo;
                }
            }

            return packageInfo;
        }
    }
}
