// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Web.LibraryManager.Cache;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Providers.Unpkg
{
    internal sealed class UnpkgProvider : BaseProvider
    {
        public const string IdText = "unpkg";
        public const string DownloadUrlFormat = "https://unpkg.com/{0}@{1}/{2}";
        private readonly INpmPackageSearch _packageSearch;
        private readonly INpmPackageInfoFactory _infoFactory;
        private ILibraryCatalog _catalog;

        public UnpkgProvider(IHostInteraction hostInteraction, CacheService cacheService, INpmPackageSearch packageSearch, INpmPackageInfoFactory infoFactory)
            :base(hostInteraction, cacheService)
        {
            _packageSearch = packageSearch;
            _infoFactory = infoFactory;
        }

        public override string Id => IdText;

        public override ILibraryCatalog GetCatalog()
        {
            return _catalog ?? (_catalog = new UnpkgCatalog(Id, LibraryNamingScheme, HostInteraction.Logger, _infoFactory, _packageSearch, _cacheService, CacheFolder));
        }

        public override string LibraryIdHintText => Resources.Text.UnpkgProviderHintText;

        /// <summary>
        /// Returns the UnpkgLibrary's name.
        /// </summary>
        /// <param name="library"></param>
        /// <returns></returns>
        public override string GetSuggestedDestination(ILibrary library)
        {
            if (library is UnpkgLibrary unpkgLibrary)
            {
                return unpkgLibrary.Name?.TrimStart('@');
            }

            return string.Empty;
        }

        protected override string GetDownloadUrl(ILibraryInstallationState state, string sourceFile)
        {
            return string.Format(DownloadUrlFormat, state.Name, state.Version, sourceFile);
        }
    }
}
