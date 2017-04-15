// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.Web.LibraryInstaller.Contracts;

#if NET45
using System.ComponentModel.Composition;
#endif

namespace Microsoft.Web.LibraryInstaller.Providers.FileSystem
{
#if NET45
    [Export(typeof(IProviderFactory))]
#endif
    internal class FileSystemProviderFactory : IProviderFactory
    {
        public IProvider CreateProvider(IHostInteraction hostInteraction)
        {
            return new FileSystemProvider(hostInteraction);
        }
    }
}
