using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
        private const string NpmPackageSearchUrl = "https://registry.npmjs.org/-/v1/search?text={0}&size=100"; // API doc at https://github.com/npm/registry/blob/master/docs/REGISTRY-API.md
        public const string NpmLatestPackgeInfoUrl = "https://registry.npmjs.org/{0}/latest";
        public const string NpmsPackageSearchUrl = "https://api.npms.io/v2/search?q={1}+scope:{0}";

        public static async Task<IEnumerable<string>> GetPackageNamesAsync(string searchTerm, CancellationToken cancellationToken)
        {
            if (searchTerm == null)
            {
                return Array.Empty<string>();
            }
            else if (searchTerm.StartsWith("@", StringComparison.Ordinal))
            {
                return await GetPackageNamesWithScopeAsync(searchTerm, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await GetPackageNamesFromSimpleQueryAsync(searchTerm, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<IEnumerable<string>> GetPackageNamesWithScopeAsync(string searchTerm, CancellationToken cancellationToken)
        {
            Debug.Assert(searchTerm.StartsWith("@", StringComparison.Ordinal));
            List<string> packageNames = new List<string>();

            int slash = searchTerm.IndexOf("/", StringComparison.Ordinal);
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
            string packageListUrl = string.Format(CultureInfo.InvariantCulture, NpmPackageSearchUrl, searchTerm);
            List<string> packageNames = new List<string>();

            try
            {
                JObject topLevelObject = await WebRequestHandler.Instance.GetJsonObjectViaGetAsync(packageListUrl, cancellationToken).ConfigureAwait(false);

                if (topLevelObject != null)
                {
                    // We get back something like this:
                    //
                    //{
                    //  "objects": [
                    //    {
                    //      "package": {
                    //        "name": "yargs",
                    //        "version": "6.6.0",
                    //        "description": "yargs the modern, pirate-themed, successor to optimist.",
                    //        "keywords": [
                    //          "argument",
                    //          "args",
                    //          "option",
                    //          "parser",
                    //          "parsing",
                    //          "cli",
                    //          "command"
                    //        ],
                    //        "date": "2016-12-30T16:53:16.023Z",
                    //        "links": {
                    //          "npm": "https://www.npmjs.com/package/yargs",
                    //          "homepage": "http://yargs.js.org/",
                    //          "repository": "https://github.com/yargs/yargs",
                    //          "bugs": "https://github.com/yargs/yargs/issues"
                    //        },
                    //        "publisher": {
                    //          "username": "bcoe",
                    //          "email": "ben@npmjs.com"
                    //        },
                    //        "maintainers": [
                    //          {
                    //            "username": "bcoe",
                    //            "email": "ben@npmjs.com"
                    //          },
                    //          {
                    //            "username": "chevex",
                    //            "email": "alex.ford@codetunnel.com"
                    //          },
                    //          {
                    //            "username": "nexdrew",
                    //            "email": "andrew@npmjs.com"
                    //          },
                    //          {
                    //            "username": "nylen",
                    //            "email": "jnylen@gmail.com"
                    //          }
                    //        ]
                    //      },
                    //      "score": {
                    //        "final": 0.9237841281241451,
                    //        "detail": {
                    //          "quality": 0.9270640902288084,
                    //          "popularity": 0.8484861649808381,
                    //          "maintenance": 0.9962706951777409
                    //        }
                    //      },
                    //      "searchScore": 100000.914
                    //    }
                    //  ],
                    //  "total": 1,
                    //  "time": "Wed Jan 25 2017 19:23:35 GMT+0000 (UTC)"
                    //}

                    JArray searchResultList = topLevelObject["objects"] as JArray;

                    if (searchResultList != null)
                    {
                        foreach (JObject searchResultObject in searchResultList.Children())
                        {
                            JObject packageEntry = searchResultObject["package"] as JObject;
                            if (packageEntry != null)
                            {
                                string currentPackageName = packageEntry["name"].ToString();
                                if (!String.IsNullOrWhiteSpace(currentPackageName))
                                {
                                    packageNames.Add(currentPackageName);
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

        public static async Task<NpmPackageInfo> GetPackageInfoAsync(string packageName, CancellationToken cancellationToken)
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
