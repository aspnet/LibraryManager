// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Providers.FileSystem
{
    internal class FileSystemProviderFactory : IProviderFactory
    {
        /// <summary>
        /// Creates an <see cref="Microsoft.Web.LibraryManager.Contracts.IProvider" /> instance.
        /// </summary>
        /// <param name="hostInteraction">The <see cref="Microsoft.Web.LibraryManager.Contracts.IHostInteraction" /> provided by the host to handle file system writes etc.</param>
        /// <returns>
        /// A <see cref="Microsoft.Web.LibraryManager.Contracts.IProvider" /> instance.
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
