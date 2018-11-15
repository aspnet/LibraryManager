// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Web.LibraryManager.Contracts;

#if NET472
using System.ComponentModel.Composition;
#endif

namespace Microsoft.Web.LibraryManager.Providers.FileSystem
{
    /// <summary>Internal use only</summary>
#if NET472
    [Export(typeof(IProviderFactory))]
#endif

    internal class FileSystemProviderFactory : IProviderFactory
    {
        /// <summary>
        /// Creates an <see cref="T:Microsoft.Web.LibraryManager.Contracts.IProvider" /> instance.
        /// </summary>
        /// <param name="hostInteraction">The <see cref="T:Microsoft.Web.LibraryManager.Contracts.IHostInteraction" /> provided by the host to handle file system writes etc.</param>
        /// <returns>
        /// A <see cref="T:Microsoft.Web.LibraryManager.Contracts.IProvider" /> instance.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">hostInteraction</exception>
        public IProvider CreateProvider(IHostInteraction hostInteraction)
        {
            if (hostInteraction == null)
            {
                throw new ArgumentNullException(nameof(hostInteraction));
            }

            return new FileSystemProvider(hostInteraction);
        }
    }
}
