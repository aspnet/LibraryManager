// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using LibraryInstaller.Contracts;

#if NET45
using System.ComponentModel.Composition;
#endif

namespace LibraryInstaller.Providers.Cdnjs
{
#if NET45
    [Export(typeof(IProviderFactory))]
#endif
    internal class CdnjsProviderFactory : IProviderFactory
    {
        public IProvider CreateProvider(IHostInteraction hostInteraction)
        {
            var provider = new CdnjsProvider();
            string storePath = Path.Combine(hostInteraction.CacheDirectory, provider.Id);
            provider.HostInteraction = hostInteraction;
            return provider;
        }
    }
}
