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
        }

        public string LibraryId => _libraryId;

        public string Version => string.Empty;
    }
}
