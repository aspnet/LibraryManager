// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using LibraryInstaller.Contracts;

namespace LibraryInstaller.Providers.Cdnjs
{
    internal class CdnjsLibraryDisplayInfo : ILibraryDisplayInfo
    {
        public CdnjsLibraryDisplayInfo(Asset asset, string libraryGroupName)
        {
            Asset = asset;
            Version = asset.Version;
            LibraryId = $"{libraryGroupName}@{asset.Version}";
        }

        public string LibraryId { get; }

        public string Version { get; }

        public Asset Asset { get; }
    }
}
