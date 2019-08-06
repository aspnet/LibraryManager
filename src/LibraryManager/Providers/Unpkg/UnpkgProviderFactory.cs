using System;
using Microsoft.Web.LibraryManager.Contracts;
using System.ComponentModel.Composition;

namespace Microsoft.Web.LibraryManager.Providers.Unpkg
{
    [Export(typeof(IProviderFactory))]
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
