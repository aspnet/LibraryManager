﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Contracts.Caching;
using Microsoft.Web.LibraryManager.LibraryNaming;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Web.LibraryManager.Providers.Unpkg
{
    internal class UnpkgCatalog : ILibraryCatalog
    {
        public const string CacheFileName = "cache.json";
        public const string LibraryFileListUrlFormat = "https://unpkg.com/{0}@{1}/?meta"; // e.g. https://unpkg.com/jquery@3.3.1/?meta
        public const string LatestLibraryVersonUrl = "https://unpkg.com/{0}/package.json"; // e.g. https://unpkg.com/jquery/package.json

        private readonly INpmPackageInfoFactory _packageInfoFactory;
        private readonly INpmPackageSearch _packageSearch;
        private readonly string _providerId;
        private readonly ILibraryNamingScheme _libraryNamingScheme;
        private readonly ILogger _logger;
        private readonly ICacheService _cacheService;
        private readonly string _cacheFolder;

        public UnpkgCatalog(string providerId, ILibraryNamingScheme namingScheme, ILogger logger, INpmPackageInfoFactory packageInfoFactory, INpmPackageSearch packageSearch, ICacheService cacheService, string cacheFolder)
        {
            _packageInfoFactory = packageInfoFactory;
            _packageSearch = packageSearch;
            _providerId = providerId;
            _libraryNamingScheme = namingScheme;
            _logger = logger;
            _cacheService = cacheService;
            _cacheFolder = cacheFolder;
        }

        public async Task<string> GetLatestVersion(string libraryName, bool includePreReleases, CancellationToken cancellationToken)
        {
            if (includePreReleases)
            {
                // Unpkg by default only shows the latest release version, so for prereleases we need to fetch all versions.
                // This is requires making an extra web request, so only do it if we need to consider prerelease versions.
                var libraryGroup = new UnpkgLibraryGroup(_packageInfoFactory, libraryName);
                string latest = (await libraryGroup.GetLibraryVersions(cancellationToken)).First();

                return latest;
            }

            string latestVersion = null;

            try
            {
                string latestLibraryVersionUrl = string.Format(LatestLibraryVersonUrl, libraryName);
                string latestCacheFile = Path.Combine(_cacheFolder, libraryName, $"{ManifestConstants.LatestVersion}.json");

                string latestJson = await _cacheService.GetContentsFromUriWithCacheFallbackAsync(latestLibraryVersionUrl,
                                                                                                 latestCacheFile,
                                                                                                 cancellationToken).ConfigureAwait(false);

                var packageObject = (JObject)JsonConvert.DeserializeObject(latestJson);

                if (packageObject != null)
                {
                    var versionValue = packageObject["version"] as JValue;
                    latestVersion = versionValue?.Value as string;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(ex.ToString(), LogLevel.Error);
            }

            return latestVersion;
        }

        public async Task<ILibrary> GetLibraryAsync(string libraryName, string version, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(libraryName) || string.IsNullOrEmpty(version))
            {
                throw new InvalidLibraryException(libraryName, _providerId);
            }

            string libraryId = _libraryNamingScheme.GetLibraryId(libraryName, version);
            if (string.Equals(version, ManifestConstants.LatestVersion, StringComparison.Ordinal))
            {
                string latestVersion = await GetLatestVersion(libraryId, includePreReleases: false, cancellationToken).ConfigureAwait(false);
                libraryId = _libraryNamingScheme.GetLibraryId(libraryName, latestVersion);
            }

            try
            {
                IEnumerable<string> libraryFiles = await GetLibraryFilesAsync(libraryName, version, cancellationToken).ConfigureAwait(false);

                return new UnpkgLibrary
                {
                    Version = version,
                    Files = libraryFiles.ToDictionary(k => k, b => false),
                    Name = libraryName,
                    ProviderId = _providerId,
                };
            }
            catch
            {
                throw new InvalidLibraryException(libraryId, _providerId);
            }
        }

        private async Task<IEnumerable<string>> GetLibraryFilesAsync(string libraryName, string version, CancellationToken cancellationToken)
        {
            var result = new List<string>();

            string libraryFileListUrl = string.Format(LibraryFileListUrlFormat, libraryName, version);
            string libraryFileListCacheFile = Path.Combine(_cacheFolder, libraryName, $"{version}-filelist.json");

            string fileList = await _cacheService.GetContentsFromCachedFileWithWebRequestFallbackAsync(libraryFileListCacheFile,
                                                                                                       libraryFileListUrl,
                                                                                                       cancellationToken).ConfigureAwait(false);

            var fileListObject = (JObject)JsonConvert.DeserializeObject(fileList);

            if (fileListObject != null)
            {
                GetFiles(fileListObject, result);
            }

            return result;
        }

        private void GetFiles(JObject fileObject, List<string> files)
        {
            // Parse JSON document returned by unpkg.com/libraryname@version/?meta
            // It's a flat list of all files, something like
            // {
            //   "package": "jquery",
            //   "version": "3.6.0",
            //   "prefix": "/",
            //   "files": [
            //     {
            //       "path": "/src/manipulation/_evalUrl.js",
            //       "size": 686,
            //       "type": "text/javascript",
            //       "integrity": "sha256-MSm8lidjXsHCKpftqTiXMOa+oa/RVxVwMB9FdCeV9oc="
            //     },
            //     {
            //       "path": "/src/data/var/acceptData.js",
            //       "size": 318,
            //       "type": "text/javascript",
            //       "integrity": "sha256-35uwg+OJk5cSTc7P7t5DWnbHmfRRVw3fma2nnvsfo3Q="
            //     },
            //     {
            //       "path": "/src/core/access.js",
            //       "size": 1314,
            //       "type": "text/javascript",
            //       "integrity": "sha256-yL5IraumlPZeArOjnjHXxhmcEmlvbBF3YrEhg4nKhM0="
            //     },

            if (files == null)
            {
                throw new ArgumentNullException(nameof(files));
            }

            if (fileObject != null)
            {
                var filesArray = fileObject["files"] as JArray;

                if (filesArray?.Count > 0)
                {
                    foreach (JObject file in filesArray.Cast<JObject>())
                    {
                        string path = file["path"]?.Value<string>();
                        if (string.IsNullOrEmpty(path))
                        {
                            continue;
                        }

                        // Don't include the leading "/" in the file paths, so you get dist/jquery.js rather than /dist/jquery.js
                        // We will want the user to always specify a relative path in the "Files" array of the library entry
                        files.Add(path.Substring(1));
                    }
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

            // Typing '@' after the library name should have version completion.

            try
            {
                // library name completion
                if (caretPosition < name.Length + 1)
                {
                    IEnumerable<NpmPackageInfo> packages = await _packageSearch.GetPackageNamesAsync(libraryNameStart, CancellationToken.None).ConfigureAwait(false);

                    foreach (NpmPackageInfo packageInfo in packages)
                    {
                        var completionItem = new CompletionItem
                        {
                            DisplayText = packageInfo.Name,
                            InsertionText = _libraryNamingScheme.GetLibraryId(packageInfo.Name, packageInfo.LatestVersion)
                        };

                        completions.Add(completionItem);
                    }

                    completionSet.CompletionType = CompletionSortOrder.AsSpecified;
                }

                // library version completion
                else
                {
                    completionSet.Start = name.Length + 1;
                    completionSet.Length = version.Length;

                    NpmPackageInfo npmPackageInfo = await _packageInfoFactory.GetPackageInfoAsync(name, CancellationToken.None).ConfigureAwait(false);

                    IList<SemanticVersion> versions = npmPackageInfo.Versions.OrderByDescending(v => v).ToList();

                    foreach (SemanticVersion semVersion in versions)
                    {
                        string versionText = semVersion.ToString();
                        var completionItem = new CompletionItem
                        {
                            DisplayText = versionText,
                            InsertionText = name + "@" + versionText
                        };

                        completions.Add(completionItem);
                    }

                    // support @latest version
                    completions.Add(new CompletionItem
                    {
                        DisplayText = ManifestConstants.LatestVersion,
                        InsertionText = _libraryNamingScheme.GetLibraryId(name, ManifestConstants.LatestVersion),
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

            try
            {
                IEnumerable<NpmPackageInfo> packages = await _packageSearch.GetPackageNamesAsync(term, CancellationToken.None).ConfigureAwait(false);
                IEnumerable<string> packageNames = packages.Select(p => p.Name);
                libraryGroups = packageNames.Select(packageName => new UnpkgLibraryGroup(_packageInfoFactory, packageName)).ToList<ILibraryGroup>();
            }
            catch (Exception ex)
            {
                _logger.Log(ex.ToString(), LogLevel.Error);
            }

            return libraryGroups;
        }
    }
}
