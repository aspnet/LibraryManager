// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Web.LibraryInstaller.Contracts;

#if NET45
using System.ComponentModel.Composition;
#endif

namespace Microsoft.Web.LibraryInstaller.Providers.Cdnjs
{
#if NET45
    [Export(typeof(IProviderFactory))]
#endif
    internal class CdnjsProviderFactory : IProviderFactory
    {
        public IProvider CreateProvider(IHostInteraction hostInteraction)
        {
            if (hostInteraction == null)
            {
                throw new ArgumentNullException(nameof(hostInteraction));
            }

            return new CdnjsProvider(hostInteraction);
        }
    }
}
