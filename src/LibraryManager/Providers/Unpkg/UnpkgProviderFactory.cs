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
        public IProvider CreateProvider(IHostInteraction hostInteraction)
        {
            if (hostInteraction == null)
            {
                throw new ArgumentNullException(nameof(hostInteraction));
            }

            return new UnpkgProvider(hostInteraction);
        }
    }
}
