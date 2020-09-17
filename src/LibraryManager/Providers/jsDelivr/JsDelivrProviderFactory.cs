// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Web.LibraryManager.Cache;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Providers.Unpkg;

namespace Microsoft.Web.LibraryManager.Providers.jsDelivr
{
    internal class JsDelivrProviderFactory : IProviderFactory
    {
        private readonly INpmPackageSearch _packageSearch;
        private readonly INpmPackageInfoFactory _packageInfoFactory;

        public JsDelivrProviderFactory(INpmPackageSearch packageSearch, INpmPackageInfoFactory packageInfoFactory)
        {
            _packageSearch = packageSearch;
            _packageInfoFactory = packageInfoFactory;
        }

        public IProvider CreateProvider(IHostInteraction hostInteraction)
        {
            if (hostInteraction == null)
            {
                throw new ArgumentNullException(nameof(hostInteraction));
            }

            return new JsDelivrProvider(hostInteraction, new CacheService(WebRequestHandler.Instance), _packageSearch, _packageInfoFactory);
        }
    }
}
