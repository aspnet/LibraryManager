// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Providers.Unpkg
{
    internal class UnpkgLibraryGroup : ILibraryGroup
    {
        private readonly INpmPackageInfoCache _infoCache;

        public UnpkgLibraryGroup(INpmPackageInfoCache infoCache, string displayName, string description = null)
        {
            _infoCache = infoCache;
            DisplayName = displayName;
            Description = description;
        }

        public string DisplayName { get; }

        public string Description { get; }

        public async Task<IEnumerable<string>> GetLibraryVersions(CancellationToken cancellationToken)
        {
            NpmPackageInfo npmPackageInfo = await _infoCache.GetPackageInfoAsync(DisplayName, CancellationToken.None);

            if (npmPackageInfo != null)
            {
                return npmPackageInfo.Versions
                    .OrderByDescending(v => v)
                    .Select(semanticVersion => semanticVersion.ToString())
                    .ToList();
            }

            return Enumerable.Empty<string>();
        }
    }
}
