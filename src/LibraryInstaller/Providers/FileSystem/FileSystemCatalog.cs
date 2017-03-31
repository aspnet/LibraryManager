// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using LibraryInstaller.Contracts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryInstaller.Providers.FileSystem
{
    internal class FileSystemCatalog : ILibraryCatalog
    {
        private string _providerId;

        public FileSystemCatalog(string providerId)
        {
            _providerId = providerId;
        }

        public Task<CompletionSpan> GetCompletionsAsync(string value, int caretPosition)
        {
            return Task.FromResult(default(CompletionSpan));
        }

        public async Task<ILibrary> GetLibraryAsync(string libraryId, CancellationToken cancellationToken)
        {
            var group = new FileSystemLibraryGroup(libraryId, _providerId);
            IReadOnlyList<ILibraryDisplayInfo> info = await group.GetDisplayInfosAsync(cancellationToken).ConfigureAwait(false);

            if (info.Count > 0 && info[0] != null)
            {
                return await info[0].GetLibraryAsync(cancellationToken).ConfigureAwait(false);
            }

            return null;
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
