// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryManager.Cache;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Resources;

namespace Microsoft.Web.LibraryManager.Providers.Cdnjs
{
    /// <summary>Internal use only</summary>
    internal sealed class CdnjsProvider : BaseProvider
    {
        private const string DownloadUrlFormat = "https://cdnjs.cloudflare.com/ajax/libs/{0}/{1}/{2}"; // https://aka.ms/ezcd7o/{0}/{1}/{2}
        public const string IdText = "cdnjs";

        private CdnjsCatalog _catalog;

        /// <summary>
        /// Initializes a new instance of the <see cref="CdnjsProvider"/> class.
        /// </summary>
        /// <param name="hostInteraction">The host interaction.</param>
        /// <param name="cacheService">The instance of a <see cref="CacheService"/> to use.</param>
        public CdnjsProvider(IHostInteraction hostInteraction, CacheService cacheService)
            :base(hostInteraction, cacheService)
        {
        }

        /// <inheritdoc />
        public override string Id => IdText;

        /// <inheritdoc />
        public override string LibraryIdHintText => Text.CdnjsLibraryIdHintText;

        /// <inheritdoc />
        public override ILibraryCatalog GetCatalog()
        {
            return _catalog ?? (_catalog = new CdnjsCatalog(this, _cacheService, LibraryNamingScheme));
        }

        /// <summary>
        /// Returns the CdnjsLibrary's Name
        /// </summary>
        /// <param name="library"></param>
        /// <returns></returns>
        public override string GetSuggestedDestination(ILibrary library)
        {
            if (library != null && library is CdnjsLibrary cdnjsLibrary)
            {
                return cdnjsLibrary.Name;
            }

            return string.Empty;
        }

        protected override string GetDownloadUrl(ILibraryInstallationState state, string sourceFile)
        {
            return string.Format(DownloadUrlFormat, state.Name, state.Version, sourceFile);
        }
    }
}
