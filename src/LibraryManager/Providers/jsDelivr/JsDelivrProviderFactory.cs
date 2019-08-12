// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Providers.Unpkg;

#if NET472
using System.ComponentModel.Composition;
#endif

namespace Microsoft.Web.LibraryManager.Providers.jsDelivr
{

#if NET472
    [Export(typeof(IProviderFactory))]
#endif
    internal class JsDelivrProviderFactory : IProviderFactory
    {
        private readonly INpmPackageSearch _packageSearch;
        private readonly INpmPackageInfoFactory _packageInfoFactory;

#if NET472
        [ImportingConstructor]
#endif
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

            return new JsDelivrProvider(hostInteraction, _packageSearch, _packageInfoFactory);
        }
    }
}
