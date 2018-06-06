using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;

namespace Microsoft.Web.LibraryManager.Providers.Unpkg
{
    internal class NpmPackageSearch
    {
        public const string NpmPackageInfoUrl = "https://registry.npmjs.org/{0}";
        public const string NpmPackageSearchUrl = "https://skimdb.npmjs.com/registry/_design/app/_view/browseAll?group_level=1&limit=10&start_key=%5B%22{0}%22%5D&end_key=%5B%22{0}z%22,%7B%7D%5D";
        public const string NpmLatestPackgeInfoUrl = "https://registry.npmjs.org/{0}/latest";
        public const string NpmsPackageSearchUrl = "https://api.npms.io/v2/search?q={1}+scope:{0}";

        public static async Task<IEnumerable<string>> GetPackageNamesAsync(string searchTerm, CancellationToken cancellationToken)
        {
            if (searchTerm.StartsWith("@"))
            {
                return await GetPackageNamesWithScopeAsync(searchTerm, cancellationToken).ConfigureAwait(false);
            }
            else if (!string.IsNullOrEmpty(searchTerm))
            {
                return await GetPackageNamesFromSimpleQueryAsync(searchTerm, cancellationToken).ConfigureAwait(false);
            }

            return new string[0];
        }

        private static async Task<IEnumerable<string>> GetPackageNamesWithScopeAsync(string searchTerm, CancellationToken cancellationToken)
        {
            Debug.Assert(searchTerm.StartsWith("@"));
            List<string> packageNames = new List<string>();

            int slash = searchTerm.IndexOf("/");
            if (slash > 0)
            {
                string scope = searchTerm.Substring(1, slash - 1);
                string packageName = searchTerm.Substring(slash + 1);

                // URL encode the values in the query to avoid a BadRequest
                scope = HttpUtility.UrlEncode(scope);
                packageName = HttpUtility.UrlEncode(packageName);

                string searchUrl = string.Format(NpmsPackageSearchUrl, scope, packageName);

                JObject packageListJsonObject = await WebRequestHandler.Instance.GetJsonObjectViaGetAsync(searchUrl, cancellationToken);

                if (packageListJsonObject != null)
                {
                    // We get back something like this:
                    // {
                    //  "total":46,
                    //  "results": [
                    //      {
                    //          "package": {
                    //              "name":"@types/d3-selection",
                    //              /* lots of other crap */
                    //          }
                    //      },
                    //      {
                    //         "package": {
                    //              "name":"@types/d3-array",
                    //          }
                    //      }, ...

                    JArray resultsValues =  packageListJsonObject["results"] as JArray;
                    if (resultsValues != null)
                    {
                        foreach (JObject packageEntry in resultsValues.Children())
                        {
                            if (packageEntry != null)
                            {
                                JObject packageDetails = packageEntry["package"] as JObject;
                                if (packageDetails != null)
                                {
                                    string currentPackageName = packageDetails.GetJObjectMemberStringValue("name");
                                    if (!String.IsNullOrWhiteSpace(currentPackageName))
                                    {
                                        packageNames.Add(currentPackageName);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return packageNames;
        }

        private static async Task<IEnumerable<string>> GetPackageNamesFromSimpleQueryAsync(string searchTerm, CancellationToken cancellationToken)
        {
            List<string> packageNames = new List<string>();

            string searchUrl = GetCustomNpmRegistryUrl() ?? NpmPackageSearchUrl;
            string packageListUrl = string.Format(searchUrl, searchTerm);

            try
            {
                JObject packageListJsonObject = await WebRequestHandler.Instance.GetJsonObjectViaGetAsync(packageListUrl, cancellationToken).ConfigureAwait(false);

                if (packageListJsonObject != null)
                {

                    // We get back something like this:
                    //
                    //{ "rows":[
                    //    {"key":["ang-google-maps"],"value":1},
                    //    {"key":["ang-google-services"],"value":1},
                    //    {"key":["ang-tangle"],"value":1},
                    //    {"key":["ang-validator"],"value":1},
                    //    {"key":["angcli"],"value":1},
                    //    {"key":["angel"],"value":1},
                    //    {"key":["angel.co"],"value":1},
                    //    {"key":["angela"],"value":1},
                    //    {"key":["angelabilities"],"value":1},
                    //    {"key":["angelabilities-exec"],"value":1}
                    //]}

                    JArray packageListJSON = packageListJsonObject["rows"] as JArray;

                    if (packageListJSON != null)
                    {
                        foreach (JObject packageEntry in packageListJSON.Children())
                        {
                            if (packageEntry != null)
                            {
                                JArray keysArray = packageEntry["key"] as JArray;
                                if (keysArray != null)
                                {
                                    foreach (JToken key in keysArray.Children())
                                    {
                                        string currentPackageName = key.ToString();
                                        if (!String.IsNullOrWhiteSpace(currentPackageName))
                                        {
                                            packageNames.Add(currentPackageName);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // TODO: telemetry.
                // should we report response failures separate from parse failures?
            }

            return packageNames;
        }

        private static string GetCustomNpmRegistryUrl()
        {
            string searchUrl = null;

            // TODO: {alexgav} - Get this working for Preview 4

            //RegistryKey registryRoot = VSRegistry.RegistryRoot(ServiceProvider.GlobalProvider, __VsLocalRegistryType.RegType_UserSettings, true);

            //if (registryRoot != null)
            //{
            //    using (RegistryKey snippetPathsKey = registryRoot.OpenSubKey(@"Languages\Language Services\JSON"))
            //    {
            //        if (snippetPathsKey != null)
            //        {
            //            searchUrl = (snippetPathsKey.GetValue("NPMPackageSearchUrl") as string);
            //        }
            //    }
            //}

            return searchUrl;
        }

        public static async Task<NpmPackageInfo> GetPackageInfoAsync(string packageName, CancellationToken cancellationToken)
        {
            NpmPackageInfo packageInfo = null;

            if (packageName.StartsWith("@"))
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
            Debug.Assert(packageName.StartsWith("@"));
            string searchName = "@" + HttpUtility.UrlEncode(packageName.Substring(1));
            NpmPackageInfo packageInfo = null;

            return await CreatePackageInfoAsync(searchName, packageInfo, cancellationToken);
        }

        private static async Task<NpmPackageInfo> GetPackageInfoForUnscopedPackageAsync(string packageName, CancellationToken cancellationToken)
        {
            NpmPackageInfo packageInfo = null;

            try
            {
                string packageInfoUrl = string.Format(NpmLatestPackgeInfoUrl, packageName);
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
                    Resources.Text.LibraryDetail_Unavailable,
                    Resources.Text.LibraryDetail_Unavailable,
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

                    packageInfo = new NpmPackageInfo(packageInfo.Name, packageInfo.Description, latestVersion, packageInfo.Author, packageInfo.Homepage, packageInfo.License, filteredSemanticVersions);
                }
            }
            catch (Exception)
            {
                packageInfo = new NpmPackageInfo(
                    packageName,
                    Resources.Text.LibraryDetail_Unavailable,
                    Resources.Text.LibraryDetail_Unavailable,
                    Resources.Text.LibraryDetail_Unavailable,
                    Resources.Text.LibraryDetail_Unavailable,
                    Resources.Text.LibraryDetail_Unavailable);
            }

            return packageInfo;
        }

        internal static IList<SemanticVersion> FilterOldPrereleaseVersions(List<SemanticVersion> semanticVersions)
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
