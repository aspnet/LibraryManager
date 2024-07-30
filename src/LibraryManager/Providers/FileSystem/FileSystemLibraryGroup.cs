// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;

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

        public Task<IEnumerable<string>> GetLibraryVersions(CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<string>>(Enumerable.Empty<string>());
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
