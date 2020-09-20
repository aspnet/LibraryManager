using System;
using Microsoft.Web.LibraryManager.Cache;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Providers.Unpkg
{
    internal class UnpkgProviderFactory : IProviderFactory
    {
        private readonly INpmPackageSearch _packageSearch;
        private readonly INpmPackageInfoFactory _packageInfoFactory;

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
