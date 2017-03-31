using LibraryInstaller.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryInstaller.Providers.Cdnjs
{
    internal class CdnjsCatalog : ILibraryCatalog
    {
        const int _days = 3;
        const string _fileName = "cache.json";
        const string _remoteApiUrl = "https://api.cdnjs.com/libraries?fields=name,description,version";
        const string _metaPackageUrlFormat = "https://api.cdnjs.com/libraries/{0}";

        private string _cacheFile;
        private string _providerStorePath;
        private string _providerId;
        private IEnumerable<CdnjsLibraryGroup> _libraryGroups;

        public CdnjsCatalog(string providerStorePath, string providerId)
        {
            _providerStorePath = providerStorePath;
            _providerId = providerId;
            _cacheFile = Path.Combine(_providerStorePath, _fileName);
        }

        public async Task<CompletionSpan> GetCompletionsAsync(string value, int caretPosition)
        {
            if (!await EnsureCatalogAsync(CancellationToken.None).ConfigureAwait(false))
                return default(CompletionSpan);

            var span = new CompletionSpan
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
                IReadOnlyList<ILibraryGroup> result = await SearchAsync(name, int.MaxValue, CancellationToken.None);

                foreach (CdnjsLibraryGroup group in result)
                {
                    var completion = new CompletionItem
                    {
                        DisplayText = group.Name,
                        InsertionText = group.Name + "@" + group.Version,
                        Description = group.Description
                    };

                    completions.Add(completion);
                }
            }

            // Version
            else
            {
                CdnjsLibraryGroup group = _libraryGroups.FirstOrDefault(g => g.Name == name);

                if (group != null)
                {
                    IReadOnlyList<ILibraryDisplayInfo> infos = await GetDisplayInfosAsync(group.Name, CancellationToken.None);

                    foreach (ILibraryDisplayInfo info in infos)
                    {
                        var completion = new CompletionItem
                        {
                            DisplayText = info.Version,
                            InsertionText = $"{name}@{info.Version}"
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
                return Enumerable.Empty<ILibraryGroup>().ToList();

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
                string groupName = group.Name;
                group.DisplayInfosTask = ct => GetDisplayInfosAsync(groupName, ct);
            }

            return results.ToList();
        }

        public async Task<ILibrary> GetLibraryAsync(string libraryId, CancellationToken cancellationToken)
        {
            try
            {
                string[] args = libraryId.Split('@');
                string name = args[0];
                string version = args[1];
                IReadOnlyList<ILibraryDisplayInfo> displayInfos = await GetDisplayInfosAsync(name, cancellationToken).ConfigureAwait(false);
                return await displayInfos.First(d => d.Version == version).GetLibraryAsync(cancellationToken);
            }
            catch (Exception)
            {
                throw new InvalidLibraryException(libraryId, _providerId);
            }
        }

        private IEnumerable<CdnjsLibraryGroup> GetSortedSearchResult(string term)
        {
            var list = new List<Tuple<int, CdnjsLibraryGroup>>();

            foreach (CdnjsLibraryGroup group in _libraryGroups)
            {
                if (group.Name.Equals(term, StringComparison.OrdinalIgnoreCase))
                    list.Add(Tuple.Create(10, group));

                else if (group.Name.StartsWith(term, StringComparison.OrdinalIgnoreCase))
                    list.Add(Tuple.Create(5, group));

                else if (group.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) > -1)
                    list.Add(Tuple.Create(1, group));
            }

            return list.OrderByDescending(t => t.Item1).Select(t => t.Item2);
        }

        private async Task<bool> EnsureCatalogAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return false;

            try
            {
                string json = await FileHelpers.GetFileTextAsync(_remoteApiUrl, _cacheFile, _days, cancellationToken).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(json))
                {
                    return false;
                }

                string obj = ((JObject)JsonConvert.DeserializeObject(json))["results"].ToString();

                _libraryGroups = JsonConvert.DeserializeObject<IEnumerable<CdnjsLibraryGroup>>(obj).ToArray();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex);
                return false;
            }
        }

        private async Task<IReadOnlyList<ILibraryDisplayInfo>> GetDisplayInfosAsync(string groupName, CancellationToken cancellationToken)
        {
            string metaFile = Path.Combine(_providerStorePath, groupName, "metadata.json");
            var list = new List<ILibraryDisplayInfo>();

            try
            {
                string url = string.Format(_metaPackageUrlFormat, groupName);
                string json = await FileHelpers.GetFileTextAsync(url, metaFile, _days, cancellationToken).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(json))
                {
                    var root = JObject.Parse(json);
                    IEnumerable<Asset> assets = JsonConvert.DeserializeObject<IEnumerable<Asset>>(root["assets"].ToString());

                    foreach (Asset asset in assets)
                    {
                        asset.DefaultFile = root["filename"]?.Value<string>();
                        var info = new CdnjsLibraryDisplayInfo(asset, groupName, _providerId);

                        list.Add(info);
                    }
                }
            }
            catch (Exception)
            {
                throw new InvalidLibraryException(groupName, _providerId);
            }

            return list;
        }
    }

    internal class Asset
    {
        public string Version;
        public string[] Files;
        public string DefaultFile;
    }
}