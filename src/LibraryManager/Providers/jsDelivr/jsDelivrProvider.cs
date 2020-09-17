// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryManager.Cache;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Providers.Unpkg;

namespace Microsoft.Web.LibraryManager.Providers.jsDelivr
{
    internal sealed class JsDelivrProvider : BaseProvider
    {
        private readonly INpmPackageSearch _packageSearch;
        private readonly INpmPackageInfoFactory _infoFactory;

        public const string IdText = "jsdelivr";
        public const string DownloadUrlFormat = "https://cdn.jsdelivr.net/npm/{0}@{1}/{2}";
        public const string DownloadUrlFormatGH = "https://cdn.jsdelivr.net/gh/{0}@{1}/{2}";

        private ILibraryCatalog _catalog;

        public JsDelivrProvider(IHostInteraction hostInteraction, CacheService cacheService, INpmPackageSearch packageSearch, INpmPackageInfoFactory infoFactory)
            :base(hostInteraction, cacheService)
        {
            _packageSearch = packageSearch;
            _infoFactory = infoFactory;
        }

        public override string Id => IdText;

        public override ILibraryCatalog GetCatalog()
        {
            return _catalog ?? (_catalog = new JsDelivrCatalog(Id, LibraryNamingScheme, HostInteraction.Logger, _infoFactory, _packageSearch, _cacheService, CacheFolder));
        }

        public override string LibraryIdHintText => Resources.Text.JsDelivrProviderHintText;

        /// <summary>
        /// Returns the JsDelivrLibrary's name.
        /// </summary>
        /// <param name="library"></param>
        /// <returns></returns>
        public override string GetSuggestedDestination(ILibrary library)
        {
            if (library is JsDelivrLibrary jsDelivrLibrary)
            {
                return jsDelivrLibrary.Name?.TrimStart('@');
            }

            return string.Empty;
        }

        protected override string GetDownloadUrl(ILibraryInstallationState state, string sourceFile)
        {
            string libraryId = LibraryNamingScheme.GetLibraryId(state.Name, state.Version);
            return string.Format(JsDelivrCatalog.IsGitHub(libraryId) ? DownloadUrlFormatGH : DownloadUrlFormat, state.Name, state.Version, sourceFile);
        }
    }
}
