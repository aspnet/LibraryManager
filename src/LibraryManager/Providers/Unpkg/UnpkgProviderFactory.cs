using System;
using Microsoft.Web.LibraryManager.Contracts;

#if NET472
using System.ComponentModel.Composition;
#endif

namespace Microsoft.Web.LibraryManager.Providers.Unpkg
{
#if NET472
    [Export(typeof(IProviderFactory))]
#endif
    internal class UnpkgProviderFactory : IProviderFactory
    {
        private readonly INpmPackageSearch _packageSearch;
        private readonly INpmPackageInfoFactory _packageInfoFactory;

#if NET472
        [ImportingConstructor]
#endif
        public UnpkgProviderFactory(INpmPackageSearch packageSearch, INpmPackageInfoFactory packageInfoFactory)
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

            return new UnpkgProvider(hostInteraction, new CacheService(WebRequestHandler.Instance), _packageSearch, _packageInfoFactory);
        }
    }
}
