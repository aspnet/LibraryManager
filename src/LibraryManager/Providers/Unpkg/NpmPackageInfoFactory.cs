using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;

#if NET472
using System.ComponentModel.Composition;
#endif

namespace Microsoft.Web.LibraryManager.Providers.Unpkg
{
#if NET472
    [Export(typeof(INpmPackageInfoFactory))]
#endif
    internal sealed class NpmPackageInfoFactory : INpmPackageInfoFactory
    {
        public const string NpmPackageInfoUrl = "https://registry.npmjs.org/{0}";
        public const string NpmLatestPackageInfoUrl = "https://registry.npmjs.org/{0}/latest";

        private readonly Dictionary<string, NpmPackageInfo> _cachedPackages = new Dictionary<string, NpmPackageInfo>();

        public async Task<NpmPackageInfo> GetPackageInfoAsync(string packageName, CancellationToken cancellationToken)
        {
            if (!_cachedPackages.TryGetValue(packageName, out NpmPackageInfo packageInfo))
            {
                packageInfo = await GetNewPackageInfoAsync(packageName, cancellationToken).ConfigureAwait(false);

                if (packageInfo != null)
                {
                    _cachedPackages[packageName] = packageInfo;
                }
            }

            return packageInfo;
        }

        private async Task<NpmPackageInfo> GetNewPackageInfoAsync(string packageName, CancellationToken cancellationToken)
        {
            NpmPackageInfo packageInfo = null;

            if (packageName.StartsWith("@", StringComparison.Ordinal))
            {
                packageInfo = await GetPackageInfoForScopedPackageAsync(packageName, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                packageInfo = await GetPackageInfoForUnscopedPackageAsync(packageName, cancellationToken).ConfigureAwait(false);
            }

            return packageInfo;
        }

        private static async Task<NpmPackageInfo> GetPackageInfoForScopedPackageAsync(string packageName, CancellationToken cancellationToken)
        {
            Debug.Assert(packageName.StartsWith("@", StringComparison.Ordinal));
            //We do string.Substring(1) to avoid encoding the leading '@' sign
            string searchName = "@" + HttpUtility.UrlEncode(packageName.Substring(1));
            NpmPackageInfo packageInfo = null;

            return await CreatePackageInfoAsync(searchName, packageInfo, cancellationToken);
        }

        private static async Task<NpmPackageInfo> GetPackageInfoForUnscopedPackageAsync(string packageName, CancellationToken cancellationToken)
        {
            NpmPackageInfo packageInfo = null;

            try
            {
                string packageInfoUrl = string.Format(NpmLatestPackageInfoUrl, packageName);
                JObject packageInfoJSON = await WebRequestHandler.Instance.GetJsonObjectViaGetAsync(packageInfoUrl, cancellationToken).ConfigureAwait(false);

                if (packageInfoJSON != null)
                {
                    packageInfo = NpmPackageInfo.Parse(packageInfoJSON);
                }
            }
            catch (Exception)
            {
                packageInfo = new NpmPackageInfo(
                    packageName,
                    Resources.Text.LibraryDetail_Unavailable,
                    Resources.Text.LibraryDetail_Unavailable);
            }

            return await CreatePackageInfoAsync(packageName, packageInfo, cancellationToken);
        }

        private static async Task<NpmPackageInfo> CreatePackageInfoAsync(string packageName, NpmPackageInfo packageInfo, CancellationToken cancellationToken)
        {
            try
            {
                string packageInfoUrl = string.Format(NpmPackageInfoUrl, packageName);
                JObject packageInfoJSON = await WebRequestHandler.Instance.GetJsonObjectViaGetAsync(packageInfoUrl, cancellationToken).ConfigureAwait(false);

                if (packageInfoJSON != null && packageInfo == null)
                {
                    packageInfo = NpmPackageInfo.Parse(packageInfoJSON);
                }

                JObject versionsList = packageInfoJSON["versions"] as JObject;
                if (versionsList != null)
                {
                    List<SemanticVersion> semanticVersions = versionsList.Properties().Select(p => SemanticVersion.Parse(p.Name)).ToList<SemanticVersion>();
                    string latestVersion = semanticVersions.Max().OriginalText;
                    IList<SemanticVersion> filteredSemanticVersions = FilterOldPrereleaseVersions(semanticVersions);

                    packageInfo = new NpmPackageInfo(packageInfo.Name, packageInfo.Description, latestVersion, filteredSemanticVersions);
                }
            }
            catch (Exception)
            {
                packageInfo = new NpmPackageInfo(
                    packageName,
                    Resources.Text.LibraryDetail_Unavailable,
                    Resources.Text.LibraryDetail_Unavailable);
            }

            return packageInfo;
        }

        private static IList<SemanticVersion> FilterOldPrereleaseVersions(List<SemanticVersion> semanticVersions)
        {
            List<SemanticVersion> filteredVersions = new List<SemanticVersion>();
            List<SemanticVersion> releasedVersions = semanticVersions.Where(sv => sv.PrereleaseVersion == null).ToList<SemanticVersion>();
            SemanticVersion latestReleaseVersion = releasedVersions.Max();

            filteredVersions.AddRange(releasedVersions);
            filteredVersions.AddRange(semanticVersions.Where(sv => sv.PrereleaseVersion != null && sv.CompareTo(latestReleaseVersion) > 0));

            return filteredVersions;
        }
    }
}
