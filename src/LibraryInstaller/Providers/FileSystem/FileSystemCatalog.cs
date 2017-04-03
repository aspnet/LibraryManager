// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using LibraryInstaller.Contracts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace LibraryInstaller.Providers.FileSystem
{
    internal class FileSystemCatalog : ILibraryCatalog
    {
        private string _providerId;

        public FileSystemCatalog(string providerId)
        {
            _providerId = providerId;
        }

        public Task<CompletionSet> GetLibraryCompletionSetAsync(string value, int caretPosition)
        {
            return Task.FromResult(default(CompletionSet));
        }

        public async Task<ILibrary> GetLibraryAsync(string libraryId, CancellationToken cancellationToken)
        {
            var group = new FileSystemLibraryGroup(libraryId, _providerId);
            IReadOnlyList<ILibraryDisplayInfo> info = await group.GetDisplayInfosAsync(cancellationToken).ConfigureAwait(false);

            if (info.Count > 0 && info[0] != null)
            {
                return new FileSystemLibrary
                {
                    Name = libraryId,
                    ProviderId = _providerId,
                    Files = GetFiles(libraryId)
                };
            }

            return null;
        }

        private IReadOnlyDictionary<string, bool> GetFiles(string libraryId)
        {
            if (Directory.Exists(libraryId))
            {
                return Directory.EnumerateFiles(libraryId)
                        .Select(f => Path.GetFileName(f))
                        .ToDictionary((k) => k, (v) => true);
            }
            else
            {
                return new Dictionary<string, bool>() { { Path.GetFileName(libraryId), true } };
            }
        }

        public Task<IReadOnlyList<ILibraryGroup>> SearchAsync(string term, int maxHits, CancellationToken cancellationToken)
        {
            var groups = new List<ILibraryGroup>()
            {
                new FileSystemLibraryGroup(term, _providerId)
            };

            return Task.FromResult<IReadOnlyList<ILibraryGroup>>(groups);
        }
    }
}
