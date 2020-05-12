// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Helpers;
using Microsoft.Web.LibraryManager.LibraryNaming;
using Microsoft.Web.LibraryManager.Providers.Unpkg;
using Newtonsoft.Json.Linq;

namespace Microsoft.Web.LibraryManager.Providers.jsDelivr
{
    internal class JsDelivrCatalog : ILibraryCatalog
    {
        public const string CacheFileName = "cache.json";
        public const string LibraryFileListUrlFormat = "https://data.jsdelivr.com/v1/package/npm/{0}/flat";
        public const string LatestLibraryVersionUrl = "https://data.jsdelivr.com/v1/package/npm/{0}";
        public const string LibraryFileListUrlFormatGH = "https://data.jsdelivr.com/v1/package/gh/{0}/flat";
        public const string LatestLibraryVersionUrlGH = "https://data.jsdelivr.com/v1/package/gh/{0}";
        public const string LatestVersionTag = "latest";

        private readonly INpmPackageInfoFactory _packageInfoFactory;
        private readonly INpmPackageSearch _packageSearch;

        private readonly string _providerId;
        private readonly ILibraryNamingScheme _libraryNamingScheme;
        private readonly ILogger _logger;
        private readonly IWebRequestHandler _webRequestHandler;

        public JsDelivrCatalog(string providerId, ILibraryNamingScheme namingScheme, ILogger logger, IWebRequestHandler webRequestHandler, INpmPackageInfoFactory packageInfoFactory, INpmPackageSearch packageSearch)
        {
            _packageInfoFactory = packageInfoFactory;
            _packageSearch = packageSearch;
            _providerId = providerId;
            _libraryNamingScheme = namingScheme;
            _logger = logger;
            _webRequestHandler = webRequestHandler;
        }

        public async Task<string> GetLatestVersion(string libraryId, bool includePreReleases, CancellationToken cancellationToken)
        {
            string latestVersion = null;

            try
            {
                (string name, string _) = _libraryNamingScheme.GetLibraryNameAndVersion(libraryId);
                string latestLibraryVersionUrl = string.Format(IsGitHub(libraryId) ? LatestLibraryVersionUrlGH : LatestLibraryVersionUrl, name);

                JObject packageObject = await _webRequestHandler.GetJsonObjectViaGetAsync(latestLibraryVersionUrl, cancellationToken);

                if (packageObject != null)
                {
                    var versions = packageObject["tags"] as JObject;
                    var versionValue = versions["latest"] as JValue;
                    latestVersion = versionValue?.Value as string;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(ex.ToString(), LogLevel.Error);
            }

            return latestVersion;
        }

        public async Task<ILibrary> GetLibraryAsync(string name, string version, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(version))
            {
                throw new InvalidLibraryException(name, _providerId);
            }

            string libraryId = _libraryNamingScheme.GetLibraryId(name, version);
            if(string.Equals(version, LatestVersionTag, StringComparison.Ordinal))
            {
                string latestVersion = await GetLatestVersion(libraryId, includePreReleases: false, cancellationToken);
                libraryId = _libraryNamingScheme.GetLibraryId(name, latestVersion);
            }

            try
            {
                IEnumerable<string> libraryFiles = await GetLibraryFilesAsync(libraryId, cancellationToken);
                return new JsDelivrLibrary { Version = version, Files = libraryFiles.ToDictionary(k => k, b => false), Name = name, ProviderId = _providerId };
            }
            catch (Exception)
            {
                throw new InvalidLibraryException(libraryId, _providerId);
            }
        }

        private async Task<IEnumerable<string>> GetLibraryFilesAsync(string libraryId, CancellationToken cancellationToken)
        {
            var result = new List<string>();

            string libraryFileListUrl = string.Format(IsGitHub(libraryId) ? LibraryFileListUrlFormatGH : LibraryFileListUrlFormat, libraryId);

            JObject fileListObject = await _webRequestHandler.GetJsonObjectViaGetAsync(libraryFileListUrl, cancellationToken).ConfigureAwait(false);

            if (fileListObject != null)
            {
                GetFiles(fileListObject, result);
            }

            return result;
        }

        private void GetFiles(JObject fileObject, List<string> result)
        {
            /*
             {
	            "default": "/lib/ip",
	            "files": [
		            {
			            "name": "/.jscsrc",
			            "hash": "ezIprgTMzY16ScfL4cx7q57jJwQqKwKHECH/dFCAA30=",
			            "time": "2015-10-29T00:56:04.000Z",
			            "size": 1623
		            },
		            {
			            "name": "/.jshintrc",
			            "hash": "XcGVRozKNw+j/Sq5xtqzthlM6jiEK+2CQFbFWl0PozA=",
			            "time": "2015-10-29T00:56:04.000Z",
			            "size": 6123
		            }
                  ]
                }
             */

            var files = new List<string>();

            if (fileObject != null)
            {
                var fileArray = fileObject["files"] as JArray;
                foreach (JToken file in fileArray)
                {
                    var pathValue = file["name"] as JValue;

                    if (pathValue?.Value is string path && path.Length > 0)
                    {
                        // Don't include the leading "/" in the file paths, so you get dist/jquery.js rather than /dist/jquery.js
                        // We will want the user to always specify a relative path in the "Files" array of the library entry
                        files.Add(path.TrimStart('/'));

                        // Add auto-minified files generated by jsDelivr which are not included in listings.
                        if (path.EndsWith(".js", StringComparison.OrdinalIgnoreCase) && !path.EndsWith(".min.js", StringComparison.OrdinalIgnoreCase))
                        {
                            files.Add(path.Substring(1, path.Length - 4) + ".min.js");
                        }
                        else if (path.EndsWith(".css", StringComparison.OrdinalIgnoreCase) && !path.EndsWith(".min.css", StringComparison.OrdinalIgnoreCase))
                        {
                            files.Add(path.Substring(1, path.Length - 5) + ".min.css");
                        }
                    }
                }

                // Make sure we don't list some minified files twice.
                files = files.Distinct().ToList();

                foreach(string file in files)
                {
                    result.Add(file.TrimStart('/'));
                }
            }
        }

        public async Task<CompletionSet> GetLibraryCompletionSetAsync(string libraryNameStart, int caretPosition)
        {
            var completions = new List<CompletionItem>();

            var completionSet = new CompletionSet
            {
                Start = 0,
                Length = 0,
                Completions = completions
            };

            if (string.IsNullOrEmpty(libraryNameStart))
            {
                // no point in doing the rest of the work, we know it's going to be an empty completion set anyway
                return completionSet;
            }

            completionSet.Length = libraryNameStart.Length;

            (string name, string version) = _libraryNamingScheme.GetLibraryNameAndVersion(libraryNameStart);

            try
            {
                // library name completion
                if (caretPosition < name.Length + 1)
                {
                    if (IsGitHub(libraryNameStart))
                    {
                        return completionSet;
                    }

                    IEnumerable<NpmPackageInfo> packages = await _packageSearch.GetPackageNamesAsync(libraryNameStart, CancellationToken.None);

                    foreach (NpmPackageInfo package in packages)
                    {
                        var completionItem = new CompletionItem
                        {
                            DisplayText = package.Name,
                            InsertionText = _libraryNamingScheme.GetLibraryId(package.Name, package.LatestVersion)
                        };

                        completions.Add(completionItem);
                    }
                }

                // library version completion
                else
                {
                    completionSet.Start = name.Length + 1;
                    completionSet.Length = version.Length;

                    IEnumerable<string> versions;

                    if (IsGitHub(name))
                    {
                        versions = await GetGithubLibraryVersionsAsync(name);
                    }
                    else
                    {
                        var libGroup = new JsDelivrLibraryGroup(_packageInfoFactory, name);
                        versions = await libGroup.GetLibraryVersions(CancellationToken.None);
                    }

                    foreach (string v in versions)
                    {
                        var completionItem = new CompletionItem
                        {
                            DisplayText = v,
                            InsertionText = _libraryNamingScheme.GetLibraryId(name, v),
                        };

                        completions.Add(completionItem);
                    }

                    // support @latest version
                    completions.Add(new CompletionItem
                    {
                        DisplayText = LatestVersionTag,
                        InsertionText = _libraryNamingScheme.GetLibraryId(name, LatestVersionTag),
                    });

                    completionSet.CompletionType = CompletionSortOrder.Version;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(ex.ToString(), LogLevel.Error);
            }

            return completionSet;
        }

        public async Task<IReadOnlyList<ILibraryGroup>> SearchAsync(string term, int maxHits, CancellationToken cancellationToken)
        {
            var libraryGroups = new List<ILibraryGroup>();

            if (IsGitHub(term))
            {
                return libraryGroups;
            }

            try
            {
                IEnumerable<NpmPackageInfo> packages = await _packageSearch.GetPackageNamesAsync(term, CancellationToken.None);
                IEnumerable<string> packageNames = packages.Select(p => p.Name);
                libraryGroups = packageNames.Select(packageName => new JsDelivrLibraryGroup(_packageInfoFactory, packageName)).ToList<ILibraryGroup>();
            }
            catch (Exception ex)
            {
                _logger.Log(ex.ToString(), LogLevel.Error);
            }

            return libraryGroups;
        }

        private async Task<IEnumerable<string>> GetGithubLibraryVersionsAsync(string name)
        {
            var versions = new List<string>();
            JObject versionsObject = await _webRequestHandler.GetJsonObjectViaGetAsync(string.Format(LatestLibraryVersionUrlGH, name), CancellationToken.None).ConfigureAwait(false);
            var versionsArray = versionsObject["versions"] as JArray;

            foreach (string version in versionsArray)
            {
                versions.Add(version);
            }

            return versions;
        }

        public static bool IsGitHub(string libraryId)
        {
            if (libraryId == null || libraryId.StartsWith("@", StringComparison.Ordinal))
            {
                return false;
            }

            if (libraryId.Contains('/'))
            {
                return true;
            }

            return false;
        }
    }
}
