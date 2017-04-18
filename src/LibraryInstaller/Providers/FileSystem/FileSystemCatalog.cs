// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryInstaller.Contracts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System;

namespace Microsoft.Web.LibraryInstaller.Providers.FileSystem
{
    internal class FileSystemCatalog : ILibraryCatalog
    {
        private readonly FileSystemProvider _provider;

        public FileSystemCatalog(FileSystemProvider provider)
        {
            _provider = provider;
        }

        public Task<CompletionSet> GetLibraryCompletionSetAsync(string value, int caretPosition)
        {
            if (value.Contains("://"))
            {
                return Task.FromResult(default(CompletionSet));
            }

            char separator = value.Contains('\\') ? '\\' : '/';
            int index = value.Length >= caretPosition - 1 ? value.LastIndexOf(separator, Math.Max(caretPosition - 1, 0)) : value.Length;
            string path = _provider.HostInteraction.WorkingDirectory;
            string prefix = "";

            if (index > 0)
            {
                prefix = value.Substring(0, index + 1);
                path = Path.Combine(path, prefix);
            }

            var set = new CompletionSet
            {
                Start = 0,
                Length = value.Length
            };

            var dir = new DirectoryInfo(path);

            if (dir.Exists)
            {
                var list = new List<CompletionItem>();

                foreach (FileSystemInfo item in dir.EnumerateDirectories())
                {
                    var completion = new CompletionItem
                    {
                        DisplayText = item.Name + separator,
                        InsertionText = prefix + item.Name + separator,
                    };

                    list.Add(completion);
                }

                foreach (FileSystemInfo item in dir.EnumerateFiles())
                {
                    var completion = new CompletionItem
                    {
                        DisplayText = item.Name,
                        InsertionText = prefix + item.Name,
                    };

                    list.Add(completion);
                }

                set.Completions = list;
            }

            return Task.FromResult(set);
        }

        public async Task<ILibrary> GetLibraryAsync(string libraryId, CancellationToken cancellationToken)
        {
            var library = new FileSystemLibrary
            {
                Name = libraryId,
                ProviderId = _provider.Id,
                Files = await GetFilesAsync(libraryId).ConfigureAwait(false)
            };

            return library;
        }

        private Task<IReadOnlyDictionary<string, bool>> GetFilesAsync(string libraryId)
        {
            return Task.Run<IReadOnlyDictionary<string, bool>>(() =>
            {
                if (Directory.Exists(libraryId))
                {
                    return Directory.EnumerateFiles(libraryId)
                            .Select(f => Path.GetFileName(f))
                            .ToDictionary((k) => k, (v) => true);
                }
                else
                {
                    return new Dictionary<string, bool>() { [Path.GetFileName(libraryId)] = true };
                }
            });
        }

        public Task<IReadOnlyList<ILibraryGroup>> SearchAsync(string term, int maxHits, CancellationToken cancellationToken)
        {
            var groups = new List<ILibraryGroup>()
            {
                new FileSystemLibraryGroup(term)
            };

            return Task.FromResult<IReadOnlyList<ILibraryGroup>>(groups);
        }

        public Task<string> GetLatestVersion(string libraryId, bool includePreReleases, CancellationToken cancellationToken)
        {
            return Task.FromResult(libraryId);
        }
    }
}
