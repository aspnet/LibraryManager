// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

namespace Microsoft.Web.LibraryManager.Providers.Cdnjs
{
    internal class CdnjsCatalog : ILibraryCatalog
    {
        // TO DO: These should become Provider properties to be passed to CacheService
        private const string FileName = "cache.json";
        public const string CatalogUrl = "https://api.cdnjs.com/libraries?fields=name,description,version";
        public const string MetaPackageUrlFormat =    "https://api.cdnjs.com/libraries/{0}?fields=filename,versions"; // {libraryName}
        public const string PackageVersionUrlFormat = "https://api.cdnjs.com/libraries/{0}/{1}?fields=files";         // {libraryName}/{version}

        private readonly string _cacheFile;
        private readonly CdnjsProvider _provider;
        private readonly ICacheService _cacheService;
        private readonly ILibraryNamingScheme _libraryNamingScheme;
        private IEnumerable<CdnjsLibraryGroup> _libraryGroups;

        public CdnjsCatalog(CdnjsProvider provider, ICacheService cacheService, ILibraryNamingScheme libraryNamingScheme)
        {
            _provider = provider;
            _cacheService = cacheService;
            _cacheFile = Path.Combine(provider.CacheFolder, FileName);
            _libraryNamingScheme = libraryNamingScheme;
        }

        public async Task<CompletionSet> GetLibraryCompletionSetAsync(string value, int caretPosition)
        {
            if (!await EnsureCatalogAsync(CancellationToken.None).ConfigureAwait(false))
            {
                return default;
            }

            var completionSet = new CompletionSet
            {
                Start = 0,
                Length = value.Length
            };

            int at = value.IndexOf('@');
            string name = at > -1 ? value.Substring(0, at) : value;

            var completions = new List<CompletionItem>();

            // Name
            if (at == -1 || caretPosition <= at)
            {
                IReadOnlyList<ILibraryGroup> result = await SearchAsync(name, int.MaxValue, CancellationToken.None).ConfigureAwait(false);

                foreach (CdnjsLibraryGroup group in result)
                {
                    var completion = new CompletionItem
                    {
                        DisplayText = group.DisplayName,
                        InsertionText = _libraryNamingScheme.GetLibraryId(group.DisplayName, group.Version),
                        Description = group.Description,
                    };

                    completions.Add(completion);
                }

                completionSet.CompletionType = CompletionSortOrder.AsSpecified;
            }

            // Version
            else
            {
                CdnjsLibraryGroup group = _libraryGroups.FirstOrDefault(g => g.DisplayName == name);

                if (group != null)
                {
                    completionSet.Start = at + 1;
                    completionSet.Length = value.Length - completionSet.Start;

                    IEnumerable<string> versions = await GetLibraryVersionsAsync(name, CancellationToken.None).ConfigureAwait(false);

                    foreach (string version in versions)
                    {
                        var completion = new CompletionItem
                        {
                            DisplayText = version,
                            InsertionText = _libraryNamingScheme.GetLibraryId(name, version),
                        };

                        completions.Add(completion);
                    }
                }

                completionSet.CompletionType = CompletionSortOrder.Version;
            }

            completionSet.Completions = completions;

            return completionSet;
        }

        public async Task<IReadOnlyList<ILibraryGroup>> SearchAsync(string term, int maxHits, CancellationToken cancellationToken)
        {
            if (!await EnsureCatalogAsync(cancellationToken).ConfigureAwait(false))
            {
                return Enumerable.Empty<ILibraryGroup>().ToList();
            }

            IEnumerable<CdnjsLibraryGroup> results;

            if (string.IsNullOrEmpty(term))
            {
                results = _libraryGroups.Take(maxHits);
            }
            else
            {
                results = GetSortedSearchResult(term).Take(maxHits);
            }

            foreach (CdnjsLibraryGroup group in results)
            {
                string groupName = group.DisplayName;
                group.DisplayInfosTask = ct => GetLibraryVersionsAsync(groupName, ct);
            }

            return results.ToList();
        }

        /// <summary>
        /// Returns a library from this catalog based on the libraryId
        /// </summary>
        /// <param name="libraryName"></param>
        /// <param name="version"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ILibrary> GetLibraryAsync(string libraryName, string version, CancellationToken cancellationToken)
        {
            string libraryId = _libraryNamingScheme.GetLibraryId(libraryName, version);

            if (string.IsNullOrEmpty(libraryName) || string.IsNullOrEmpty(version))
            {
                throw new InvalidLibraryException(libraryId, _provider.Id);
            }

            if (!(await GetLibraryVersionsAsync(libraryName, cancellationToken)).Contains(version))
            {
                throw new InvalidLibraryException(libraryId, _provider.Id);
            }

            try
            {
                JObject groupMetadata = await GetLibraryGroupMetadataAsync(libraryName, cancellationToken);
                string defaultFile = groupMetadata?["filename"].Value<string>() ?? string.Empty;

                IEnumerable<string> libraryFiles= await GetLibraryFilesAsync(libraryName, version, cancellationToken).ConfigureAwait(false);

                return new CdnjsLibrary
                {
                    Version = version,
                    Files = libraryFiles.ToDictionary(k => k, b => b == defaultFile),
                    Name = libraryName,
                    ProviderId = _provider.Id,
                };
            }
            catch
            {
                throw new InvalidLibraryException(libraryId, _provider.Id);
            }
        }

        public async Task<string> GetLatestVersion(string libraryName, bool includePreReleases, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(libraryName))
            {
                return null;
            }

            if (!await EnsureCatalogAsync(cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            CdnjsLibraryGroup group = _libraryGroups.FirstOrDefault(l => l.DisplayName == libraryName);

            if (group == null)
            {
                return null;
            }

            string first = includePreReleases
                ? (await GetLibraryVersionsAsync(group.DisplayName, cancellationToken).ConfigureAwait(false))
                                                                                      .Select(v => SemanticVersion.Parse(v))
                                                                                      .Max()
                                                                                      .ToString()
                : group.Version;

            if (!string.IsNullOrEmpty(first))
            {
                return first;
            }

            return null;
        }

        private IEnumerable<CdnjsLibraryGroup> GetSortedSearchResult(string term)
        {
            var list = new List<Tuple<int, CdnjsLibraryGroup>>();

            foreach (CdnjsLibraryGroup group in _libraryGroups)
            {
                string cleanName = NormalizedGroupName(group.DisplayName);

                if (cleanName.Equals(term, StringComparison.OrdinalIgnoreCase))
                    list.Add(Tuple.Create(50, group));
                else if (group.DisplayName.StartsWith(term, StringComparison.OrdinalIgnoreCase))
                    list.Add(Tuple.Create(20 + (term.Length - cleanName.Length), group));
                else if (group.DisplayName.IndexOf(term, StringComparison.OrdinalIgnoreCase) > -1)
                    list.Add(Tuple.Create(1, group));
            }

            return list.OrderByDescending(t => t.Item1).Select(t => t.Item2);
        }

        private static string NormalizedGroupName(string groupName)
        {
            if (groupName.EndsWith("js", StringComparison.OrdinalIgnoreCase))
            {
                groupName = groupName
                    .Substring(0, groupName.Length - 2)
                    .TrimEnd('-', '.');
            }

            return groupName;
        }

        private async Task<bool> EnsureCatalogAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            try
            {
                string json = await _cacheService.GetContentsFromUriWithCacheFallbackAsync(CatalogUrl, _cacheFile, cancellationToken).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(json))
                {
                    return false;
                }

                _libraryGroups = ConvertToLibraryGroups(json);

                return _libraryGroups != null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex);
                return false;
            }
        }

        private async Task<IEnumerable<string>> GetLibraryVersionsAsync(string groupName, CancellationToken cancellationToken)
        {
            JObject root = await GetLibraryGroupMetadataAsync(groupName, cancellationToken).ConfigureAwait(false);
            if (root != null && root["versions"] is JToken versionsToken)
            {
                return versionsToken.Values<string>();
            }

            return Array.Empty<string>();
        }

        private async Task<JObject> GetLibraryGroupMetadataAsync(string groupName, CancellationToken cancellationToken)
        {
            string localFile = Path.Combine(_provider.CacheFolder, groupName, "metadata.json");
            string url = string.Format(MetaPackageUrlFormat, groupName);

            try
            {
                string json = await _cacheService.GetContentsFromUriWithCacheFallbackAsync(url, localFile, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(json))
                {
                    return JObject.Parse(json);
                }
            }
            catch
            {
                throw new InvalidLibraryException(groupName, _provider.Id);
            }

            return null;
        }

        private async Task<IEnumerable<string>> GetLibraryFilesAsync(string libraryName, string version, CancellationToken cancellationToken)
        {
            string localFile = Path.Combine(_provider.CacheFolder, libraryName, $"{version}-metadata.json");
            string url = string.Format(PackageVersionUrlFormat, libraryName, version);

            IEnumerable<string> libraryFiles = Array.Empty<string>();
            try
            {

                string json = await _cacheService.GetContentsFromUriWithCacheFallbackAsync(url, localFile, cancellationToken).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(json))
                {
                    JObject root = JObject.Parse(json);

                    if (root != null && root["files"] is JArray array)
                    {
                        libraryFiles = root["files"].Values<string>();
                    }
                }
            }
            catch (Exception)
            {
                throw new InvalidLibraryException(libraryName, _provider.Id);
            }

            return libraryFiles;
        }

        internal IEnumerable<CdnjsLibraryGroup> ConvertToLibraryGroups(string json)
        {
            try
            {
                string obj = ((JObject)JsonConvert.DeserializeObject(json))["results"].ToString();

                return JsonConvert.DeserializeObject<IEnumerable<CdnjsLibraryGroup>>(obj).ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex);

                return null;
            }
        }
    }
}
