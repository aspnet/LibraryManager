using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Helpers;

namespace Microsoft.Web.LibraryManager.Providers.Unpkg
{
    internal class UnpkgLibraryGroup : ILibraryGroup
    {
        public UnpkgLibraryGroup(string displayName, string description = null)
        {
            DisplayName = displayName;
            Description = description;
        }
        public string DisplayName { get; }

        public string Description { get; }

        public async Task<IEnumerable<string>> GetLibraryIdsAsync(CancellationToken cancellationToken)
        {
            NpmPackageInfo npmPackageInfo = await NpmPackageInfoCache.GetPackageInfoAsync(DisplayName, CancellationToken.None);

            if (npmPackageInfo != null)
            {
                return npmPackageInfo.Versions
                    .OrderByDescending(v => v)
                    .Select(semanticVersion => LibraryNamingScheme.Instance.GetLibraryId(DisplayName, semanticVersion.ToString()));
            }

            return Enumerable.Empty<string>();
        }
    }
}
