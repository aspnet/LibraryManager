using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Helpers;
using Newtonsoft.Json.Linq;

namespace Microsoft.Web.LibraryManager.Providers.Unpkg
{
    internal sealed class NpmPackageSearch : INpmPackageSearch
    {
        private const string NpmPackageSearchUrl = "https://registry.npmjs.org/-/v1/search?text={0}&size=100"; // API doc at https://github.com/npm/registry/blob/master/docs/REGISTRY-API.md
        public const string NpmsPackageSearchUrl = "https://api.npms.io/v2/search?q={1}+scope:{0}";

        private readonly IWebRequestHandler _requestHandler;

        public NpmPackageSearch(IWebRequestHandler webRequestHandler)
        {
            _requestHandler = webRequestHandler;
        }

        public async Task<IEnumerable<NpmPackageInfo>> GetPackageNamesAsync(string searchTerm, CancellationToken cancellationToken)
        {
            if (searchTerm == null)
            {
                return Array.Empty<NpmPackageInfo>();
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

        private async Task<IEnumerable<NpmPackageInfo>> GetPackageNamesWithScopeAsync(string searchTerm, CancellationToken cancellationToken)
        {
            Debug.Assert(searchTerm.StartsWith("@", StringComparison.Ordinal));
            var packages = new List<NpmPackageInfo>();

            int slash = searchTerm.IndexOf("/", StringComparison.Ordinal);
            if (slash > 0)
            {
                string scope = searchTerm.Substring(1, slash - 1);
                string packageName = searchTerm.Substring(slash + 1);

                // URL encode the values in the query to avoid a BadRequest
                scope = HttpUtility.UrlEncode(scope);
                packageName = HttpUtility.UrlEncode(packageName);

                string searchUrl = string.Format(NpmsPackageSearchUrl, scope, packageName);

                JObject packageListJsonObject = await _requestHandler.GetJsonObjectViaGetAsync(searchUrl, cancellationToken);

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

                    if (packageListJsonObject["results"] is JArray resultsValues)
                    {
                        foreach (JObject packageEntry in resultsValues.Children())
                        {
                            if (packageEntry != null)
                            {
                                if (packageEntry["package"] is JObject packageDetails)
                                {
                                    var packageInfo = NpmPackageInfo.Parse(packageDetails);
                                    packages.Add(packageInfo);
                                }
                            }
                        }
                    }
                }
            }

            return packages;
        }

        private async Task<IEnumerable<NpmPackageInfo>> GetPackageNamesFromSimpleQueryAsync(string searchTerm, CancellationToken cancellationToken)
        {
            string packageListUrl = string.Format(CultureInfo.InvariantCulture, NpmPackageSearchUrl, searchTerm);
            var packages = new List<NpmPackageInfo>();

            try
            {
                JObject topLevelObject = await _requestHandler.GetJsonObjectViaGetAsync(packageListUrl, cancellationToken).ConfigureAwait(false);

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

                    if (topLevelObject["objects"] is JArray searchResultList)
                    {
                        foreach (JObject searchResultObject in searchResultList.Children())
                        {
                            if (searchResultObject["package"] is JObject packageEntry)
                            {
                                var packageInfo = NpmPackageInfo.Parse(packageEntry);
                                packages.Add(packageInfo);
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

            return packages;
        }
    }
}
