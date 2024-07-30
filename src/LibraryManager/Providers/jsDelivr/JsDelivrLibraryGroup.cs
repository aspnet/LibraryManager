// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Providers.Unpkg;

namespace Microsoft.Web.LibraryManager.Providers.jsDelivr
{
    internal class JsDelivrLibraryGroup : ILibraryGroup
    {
        private readonly INpmPackageInfoFactory _infoCache;

        public JsDelivrLibraryGroup(INpmPackageInfoFactory infoCache, string displayName, string description = null)
        {
            _infoCache = infoCache;
            DisplayName = displayName;
            Description = description;
        }

        public string DisplayName { get; }

        public string Description { get; }

        public async Task<IEnumerable<string>> GetLibraryVersions(CancellationToken cancellationToken)
        {

            if (!JsDelivrCatalog.IsGitHub(DisplayName))
            {
                NpmPackageInfo npmPackageInfo = await _infoCache.GetPackageInfoAsync(DisplayName, CancellationToken.None);

                if (npmPackageInfo != null)
                {
                    return npmPackageInfo.Versions
                        .OrderByDescending(v => v)
                        .Select(v => v.ToString())
                        .ToList();
                }
            }

            return Enumerable.Empty<string>();
        }
    }
}
