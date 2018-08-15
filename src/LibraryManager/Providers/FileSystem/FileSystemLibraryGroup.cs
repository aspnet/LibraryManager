// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryManager.Contracts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Providers.FileSystem
{
    internal class FileSystemLibraryGroup : ILibraryGroup
    {
        public FileSystemLibraryGroup(string groupName)
        {
            DisplayName = groupName;
        }

        public string DisplayName { get; }

        public string Description => string.Empty;

        public Task<IEnumerable<string>> GetLibraryIdsAsync(CancellationToken cancellationToken)
        {
            string[] ids = { DisplayName };

            return Task.FromResult<IEnumerable<string>>(ids);
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
