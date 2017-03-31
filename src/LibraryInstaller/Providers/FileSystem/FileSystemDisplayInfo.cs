// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using LibraryInstaller.Contracts;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryInstaller.Providers.FileSystem
{
    internal class FileSystemDisplayInfo : ILibraryDisplayInfo
    {
        private string _libraryId;
        private string _providerId;
        private Dictionary<string, bool> _files = new Dictionary<string, bool>();

        public FileSystemDisplayInfo(string libraryId, string providerId)
        {
            _libraryId = libraryId;
            _providerId = providerId;
            GetFiles(libraryId);
        }

        public string Version => string.Empty;

        public Task<ILibrary> GetLibraryAsync(CancellationToken cancellationToken)
        {
            var library = new FileSystemLibrary
            {
                Name = _libraryId,
                ProviderId = _providerId,
                Files = _files
            };

            return Task.FromResult<ILibrary>(library);
        }

        private void GetFiles(string libraryId)
        {
            if (Directory.Exists(libraryId))
            {
                _files = Directory.EnumerateFiles(libraryId)
                        .Select(f => Path.GetFileName(f))
                        .ToDictionary((k) => k, (v) => true);
            }
            else
            {
                _files.Add(Path.GetFileName(libraryId), true);
            }
        }
    }
}
