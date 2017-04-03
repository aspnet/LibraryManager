// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using LibraryInstaller.Contracts;

namespace LibraryInstaller.Providers.FileSystem
{
    internal class FileSystemDisplayInfo : ILibraryDisplayInfo
    {
        public FileSystemDisplayInfo(string libraryId)
        {
            LibraryId = libraryId;
        }

        public string LibraryId { get; }

        public string Version => string.Empty;
    }
}
