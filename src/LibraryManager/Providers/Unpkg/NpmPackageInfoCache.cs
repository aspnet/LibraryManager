// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Providers.Unpkg
{
    internal static class NpmPackageInfoCache
    {
        private static readonly Dictionary<string, NpmPackageInfo> CachedPackages = new Dictionary<string, NpmPackageInfo>();

        internal static async Task<NpmPackageInfo> GetPackageInfoAsync(string packageName, CancellationToken cancellationToken)
        {
            if (!CachedPackages.TryGetValue(packageName, out NpmPackageInfo packageInfo))
            {
                packageInfo = await NpmPackageSearch.GetPackageInfoAsync(packageName, cancellationToken).ConfigureAwait(false);

                if (packageInfo != null)
                {
                    CachedPackages[packageName] = packageInfo;
                }
            }

            return packageInfo;
        }
    }
}
