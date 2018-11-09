// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.LibraryNaming;
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
        private JsDelivrProvider _provider;
        private CacheService _cacheService;
        private string _cacheFile;


        public JsDelivrCatalog(JsDelivrProvider provider)
        {
            _provider = provider;
            _cacheService = new CacheService(WebRequestHandler.Instance);
            _cacheFile = Path.Combine(provider.CacheFolder, CacheFileName);
        }

        public async Task<string> GetLatestVersion(string libraryId, bool includePreReleases, CancellationToken cancellationToken)
        {
            string latestVersion = null;

            try
            {
                (string name, string version) = LibraryIdToNameAndVersionConverter.Instance.GetLibraryNameAndVersion(libraryId, _provider.Id);
                string latestLibraryVersionUrl = string.Format(IsGitHub(libraryId) ? LatestLibraryVersionUrlGH : LatestLibraryVersionUrl, name);

                JObject packageObject = await WebRequestHandler.Instance.GetJsonObjectViaGetAsync(latestLibraryVersionUrl, cancellationToken);

                if (packageObject != null)
                {
                    JObject versions = packageObject["tags"] as JObject;
                    JValue versionValue = versions["latest"] as JValue;
                    latestVersion = versionValue?.Value as string;
                }
            }
            catch (Exception ex)
            {
                _provider.HostInteraction.Logger.Log(ex.ToString(), LogLevel.Error);
            }

            return latestVersion;
        }

        public async Task<ILibrary> GetLibraryAsync(string name, string version, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(version))
            {
                throw new InvalidLibraryException(name, _provider.Id);
            }

            string libraryId = LibraryIdToNameAndVersionConverter.Instance.GetLibraryId(name, version, _provider.Id);

            try
            {
                IEnumerable<string> libraryFiles = await GetLibraryFilesAsync(libraryId, cancellationToken);
                return new JsDelivrLibrary { Version = version, Files = libraryFiles.ToDictionary(k => k, b => false), Name = name, ProviderId = _provider.Id };
            }
            catch (Exception)
            {
                throw new InvalidLibraryException(libraryId, _provider.Id);
            }
        }

        private async Task<IEnumerable<string>> GetLibraryFilesAsync(string libraryId, CancellationToken cancellationToken)
        {
            List<string> result = new List<string>();

            string libraryFileListUrl = string.Format(IsGitHub(libraryId) ? LibraryFileListUrlFormatGH : LibraryFileListUrlFormat, libraryId);
            JObject fileListObject = await WebRequestHandler.Instance.GetJsonObjectViaGetAsync(libraryFileListUrl, cancellationToken).ConfigureAwait(false);

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

            List<string> files = new List<string>();

            if (files == null)
            {
                throw new ArgumentNullException(nameof(files));
            }

            if (fileObject != null)
            {
                JArray fileArray = fileObject["files"] as JArray;
                foreach (JToken file in fileArray)
                {
                    JValue pathValue = file["name"] as JValue;
                    string path = pathValue?.Value as string;
                    
                    if (path != null && path.Length > 0)
                    {
                        // Don't include the leading "/" in the file paths, so you get dist/jquery.js rather than /dist/jquery.js
                        // We will want the user to always specify a relative path in the "Files" array of the library entry
                        files.Add(path.TrimStart('/'));

                        // Add auto-minified files generated by jsDelivr which are not included in listings.
                        if (path.EndsWith(".js") && !path.EndsWith(".min.js"))
                        {
                            files.Add(path.Substring(1, path.Length - 4) + ".min.js");
                        }
                        else if (path.EndsWith(".css") && !path.EndsWith(".min.css"))
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
            CompletionSet completionSet = new CompletionSet
            {
                Start = 0,
                Length = libraryNameStart.Length
            };

            List<CompletionItem> completions = new List<CompletionItem>();

            (string name, string version) = LibraryIdToNameAndVersionConverter.Instance.GetLibraryNameAndVersion(libraryNameStart, _provider.Id);

            try
            {
                // library name completion
                if (caretPosition < name.Length + 1 && name[name.Length - 1] != '@')
                {
                    if (IsGitHub(libraryNameStart))
                    {
                        return completionSet;
                    }

                    IEnumerable<string> packageNames = await Microsoft.Web.LibraryManager.Providers.Unpkg.NpmPackageSearch.GetPackageNamesAsync(libraryNameStart, CancellationToken.None);

                    foreach (string packageName in packageNames)
                    {
                        CompletionItem completionItem = new CompletionItem
                        {
                            DisplayText = packageName,
                            InsertionText = packageName
                        };

                        completions.Add(completionItem);
                    }
                }

                // library version completion
                else
                {
                    name = name[name.Length - 1] == '@' ? name.Remove(name.Length - 1) : name;

                    completionSet.Start = name.Length + 1;
                    completionSet.Length = version.Length;

                    IEnumerable<string> versions;

                    if (IsGitHub(name))
                    {
                        versions = await GetGithubLibraryVersionsAsync(name);
                    }
                    else
                    {
                        JsDelivrLibraryGroup libGroup = new JsDelivrLibraryGroup(name);
                        versions = await libGroup.GetLibraryVersions(CancellationToken.None);
                    }

                    foreach (string v in versions)
                    {
                        CompletionItem completionItem = new CompletionItem
                        {
                            DisplayText = v,
                            InsertionText = LibraryIdToNameAndVersionConverter.Instance.GetLibraryId(name, v, _provider.Id)
                        };

                        completions.Add(completionItem);
                    }
                }

                completionSet.Completions = completions;
            }
            catch (Exception ex)
            {
                _provider.HostInteraction.Logger.Log(ex.ToString(), LogLevel.Error);
            }

            return completionSet;
        }

        public async Task<IReadOnlyList<ILibraryGroup>> SearchAsync(string term, int maxHits, CancellationToken cancellationToken)
        {
            List<ILibraryGroup> libraryGroups = new List<ILibraryGroup>();

            if (IsGitHub(term))
            {
                return libraryGroups;
            }

            try
            {
                IEnumerable<string> packageNames = await Microsoft.Web.LibraryManager.Providers.Unpkg.NpmPackageSearch.GetPackageNamesAsync(term, CancellationToken.None);
                libraryGroups = packageNames.Select(packageName => new JsDelivrLibraryGroup(packageName)).ToList<ILibraryGroup>();
            }
            catch (Exception ex)
            {
                _provider.HostInteraction.Logger.Log(ex.ToString(), LogLevel.Error);
            }

            return libraryGroups;
        }

        private async Task<IEnumerable<string>> GetGithubLibraryVersionsAsync(string name)
        {
            List<string> versions = new List<string>();
            JObject versionsObject = await WebRequestHandler.Instance.GetJsonObjectViaGetAsync(string.Format(LatestLibraryVersionUrlGH, name), CancellationToken.None).ConfigureAwait(false);
            JArray versionsArray = versionsObject["versions"] as JArray;

            foreach (string version in versionsArray)
            {
                versions.Add(version);
            }

            return versions;
        }

        public static bool IsGitHub(string libraryId)
        {
            if (libraryId == null || libraryId.StartsWith("@"))
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
