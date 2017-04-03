// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using LibraryInstaller.Contracts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryInstaller.Providers.FileSystem
{
    internal class FileSystemLibraryGroup : ILibraryGroup
    {
        public FileSystemLibraryGroup(string groupName)
        {
            DisplayName = groupName;
        }

        public string DisplayName { get; }

        public string Description => string.Empty;

        public Task<IReadOnlyList<ILibraryDisplayInfo>> GetDisplayInfosAsync(CancellationToken cancellationToken)
        {
            var infos = new List<ILibraryDisplayInfo>
            {
                new FileSystemDisplayInfo(DisplayName)
            };

            return Task.FromResult<IReadOnlyList<ILibraryDisplayInfo>>(infos);
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
