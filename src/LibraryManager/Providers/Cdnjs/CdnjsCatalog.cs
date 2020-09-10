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
        private const string RemoteApiUrl = "https://aka.ms/g8irvu";
        private const string MetaPackageUrlFormat = "https://api.cdnjs.com/libraries/{0}"; // https://aka.ms/goycwu/{0}

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

                    IEnumerable<Asset> assets = await GetAssetsAsync(name, CancellationToken.None).ConfigureAwait(false);

                    foreach (string version in assets.Select(a => a.Version))
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

            try
            {
                IEnumerable<Asset> assets = await GetAssetsAsync(libraryName, cancellationToken).ConfigureAwait(false);
                Asset asset = assets.FirstOrDefault(a => a.Version == version);

                if (asset == null)
                {
                    throw new InvalidLibraryException(libraryId, _provider.Id);
                }

                return new CdnjsLibrary
                {
                    Version = version,
                    Files = asset.Files.ToDictionary(k => k, b => b == asset.DefaultFile),
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
                ? (await GetLibraryVersionsAsync(group.DisplayName, cancellationToken).ConfigureAwait(false)).First()
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
                string json;
                try
                {
                    json = await _cacheService.GetCatalogAsync(RemoteApiUrl, _cacheFile, cancellationToken).ConfigureAwait(false);
                }
                catch (ResourceDownloadException)
                {
                    // TODO: add telemetry
                    _provider.HostInteraction.Logger.Log(string.Format(Resources.Text.FailedToDownloadCatalog, _provider.Id), LogLevel.Operation);
                    if (File.Exists(_cacheFile))
                    {
                        json = await FileHelpers.ReadFileAsTextAsync(_cacheFile, cancellationToken);
                    }
                    else
                    {
                        return false;
                    }
                }

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
            IEnumerable<Asset> assets = await GetAssetsAsync(groupName, cancellationToken).ConfigureAwait(false);

            return assets?.Select(a => a.Version);
        }

        private async Task<IEnumerable<Asset>> GetAssetsAsync(string groupName, CancellationToken cancellationToken)
        {
            var assets = new List<Asset>();
            string localFile = Path.Combine(_provider.CacheFolder, groupName, "metadata.json");
            string url = string.Format(MetaPackageUrlFormat, groupName);

            try
            {
                string json;
                try
                {
                    json = await _cacheService.GetMetadataAsync(url, localFile, cancellationToken).ConfigureAwait(false);
                }
                catch (ResourceDownloadException)
                {
                    // TODO: Log telemetry
                    if (File.Exists(localFile))
                    {
                        json = await FileHelpers.ReadFileAsTextAsync(localFile, cancellationToken);
                    }
                    else
                    {
                        throw;
                    }
                }

                if (!string.IsNullOrEmpty(json))
                {
                    assets = ConvertToAssets(json);

                    if (assets == null)
                    {
                        throw new Exception();
                    }
                }
            }
            catch (Exception)
            {
                throw new InvalidLibraryException(groupName, _provider.Id);
            }

            return assets;
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

        internal List<Asset> ConvertToAssets(string json)
        {
            try
            {
                var assets = new List<Asset>();

                var root = JObject.Parse(json);
                if (root["assets"] != null)
                {
                    assets = JsonConvert.DeserializeObject<IEnumerable<Asset>>(root["assets"].ToString()).ToList();
                    string defaultFileName = root["filename"]?.Value<string>();

                    if (assets != null)
                    {
                        assets.ForEach(a => a.DefaultFile = defaultFileName);
                    }
                }

                return assets;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    internal class Asset
    {
        public string Version { get; set; }
        public string[] Files { get; set; }
        public string DefaultFile { get; set; }
    }
}
