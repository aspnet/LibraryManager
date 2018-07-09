// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Web.LibraryManager.Providers.Cdnjs
{
    internal class CdnjsCatalog : ILibraryCatalog
    {
        // TO DO: These should become Provider properties to be passed to CacheService
        private const string _fileName = "cache.json";
        private const string _remoteApiUrl = "https://aka.ms/g8irvu";
        private const string _metaPackageUrlFormat = "https://api.cdnjs.com/libraries/{0}"; // https://aka.ms/goycwu/{0}

        private readonly string _cacheFile;
        private readonly CdnjsProvider _provider;
        private IEnumerable<CdnjsLibraryGroup> _libraryGroups;
        private CacheService _cacheService;

        public CdnjsCatalog(CdnjsProvider provider)
        {
            _provider = provider;
            _cacheService = new CacheService(WebRequestHandler.Instance);
            _cacheFile = Path.Combine(provider.CacheFolder, _fileName);
        }

        public async Task<CompletionSet> GetLibraryCompletionSetAsync(string value, int caretPosition)
        {
            if (!await EnsureCatalogAsync(CancellationToken.None).ConfigureAwait(false))
            {
                return default(CompletionSet);
            }

            var span = new CompletionSet
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
                        InsertionText = group.DisplayName + "@" + group.Version,
                        Description = group.Description,
                    };

                    completions.Add(completion);
                }
            }

            // Version
            else
            {
                CdnjsLibraryGroup group = _libraryGroups.FirstOrDefault(g => g.DisplayName == name);

                if (group != null)
                {
                    span.Start = at + 1;
                    span.Length = value.Length - span.Start;

                    IEnumerable<Asset> assets = await GetAssetsAsync(name, CancellationToken.None).ConfigureAwait(false);

                    foreach (string version in assets.Select(a => a.Version))
                    {
                        var completion = new CompletionItem
                        {
                            DisplayText = version,
                            InsertionText = $"{name}@{version}",
                        };

                        completions.Add(completion);
                    }
                }
            }

            span.Completions = completions;

            return span;
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
                group.DisplayInfosTask = ct => GetLibraryIdsAsync(groupName, ct);
            }

            return results.ToList();
        }

        /// <summary>
        /// Returns a library from this catalog based on the libraryId
        /// </summary>
        /// <param name="libraryId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ILibrary> GetLibraryAsync(string libraryId, CancellationToken cancellationToken)
        {
            (string name, string version) = LibraryNamingScheme.Instance.GetLibraryNameAndVersion(libraryId);

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(version))
            {
                throw new InvalidLibraryException(libraryId, _provider.Id);
            }

            try
            {
                IEnumerable<Asset> assets = await GetAssetsAsync(name, cancellationToken).ConfigureAwait(false);
                Asset asset = assets.FirstOrDefault(a => a.Version == version);

                if (asset == null)
                {
                    throw new InvalidLibraryException(libraryId, _provider.Id);
                }

                return new CdnjsLibrary
                {
                    Version = asset.Version,
                    Files = asset.Files.ToDictionary(k => k, b => b == asset.DefaultFile),
                    Name = name,
                    ProviderId = _provider.Id,
                };
            }
            catch
            {
                throw new InvalidLibraryException(libraryId, _provider.Id);
            }

        }

        public async Task<string> GetLatestVersion(string libraryId, bool includePreReleases, CancellationToken cancellationToken)
        {
            (string name, string version) = LibraryNamingScheme.Instance.GetLibraryNameAndVersion(libraryId);

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(version))
            {
                return null;
            }

            if (!await EnsureCatalogAsync(cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            CdnjsLibraryGroup group = _libraryGroups.FirstOrDefault(l => l.DisplayName == name);

            if (group == null)
            {
                return null;
            }

            var ids = (await GetLibraryIdsAsync(group.DisplayName, cancellationToken).ConfigureAwait(false)).ToList();
            string first = ids[0];

            if (!includePreReleases)
            {
                first = ids.First(id => id.Substring(name.Length).Any(c => !char.IsLetter(c)));
            }

            if (!string.IsNullOrEmpty(first))
            {
                return LibraryNamingScheme.Instance.GetLibraryNameAndVersion(first).Version;
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
            if (groupName.EndsWith("js"))
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
                string json = await _cacheService.GetCatalogAsync(_remoteApiUrl, _cacheFile, cancellationToken).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(json))
                {
                    return false;
                }

                string obj = ((JObject)JsonConvert.DeserializeObject(json))["results"].ToString();

                _libraryGroups = JsonConvert.DeserializeObject<IEnumerable<CdnjsLibraryGroup>>(obj).ToArray();
                return true;
            }
            catch (ResourceDownloadException)
            {
                _provider.HostInteraction.Logger.Log(string.Format(Resources.Text.FailedToDownloadCatalog, _provider.Id), LogLevel.Operation);
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex);
                return false;
            }
        }

        private async Task<IEnumerable<string>> GetLibraryIdsAsync(string groupName, CancellationToken cancellationToken)
        {
            IEnumerable<Asset> assets = await GetAssetsAsync(groupName, cancellationToken).ConfigureAwait(false);

            return assets?.Select(a => $"{groupName}@{a.Version}");
        }

        private async Task<IEnumerable<Asset>> GetAssetsAsync(string groupName, CancellationToken cancellationToken)
        {
            var list = new List<Asset>();
            string localFile = Path.Combine(_provider.CacheFolder, groupName, "metadata.json");
            string url = string.Format(_metaPackageUrlFormat, groupName);

            try
            {
                string json = await _cacheService.GetMetadataAsync(url, localFile, cancellationToken).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(json))
                {
                    var root = JObject.Parse(json);
                    if (root["assets"] != null)
                    {
                        IEnumerable<Asset> assets = JsonConvert.DeserializeObject<IEnumerable<Asset>>(root["assets"].ToString());
                        string defaultFileName = root["filename"]?.Value<string>();

                        foreach (Asset asset in assets)
                        {
                            asset.DefaultFile = defaultFileName;
                            list.Add(asset);
                        }
                    }
                }
            }
            catch (ResourceDownloadException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new InvalidLibraryException(groupName, _provider.Id);
            }

            return list;
        }
    }

    internal class Asset
    {
        public string Version { get; set; }
        public string[] Files { get; set; }
        public string DefaultFile { get; set; }
    }
}
