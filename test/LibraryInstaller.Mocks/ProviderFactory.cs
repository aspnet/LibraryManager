// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryInstaller.Contracts;

namespace Microsoft.Web.LibraryInstaller.Mocks
{
    /// <summary>
    /// A mock <see cref="IProviderFactory"/> class for use in unit tests.
    /// </summary>
    /// <seealso cref="LibraryInstaller.Contracts.IProviderFactory" />
    public class ProviderFactory : IProviderFactory
    {
        /// <summary>
        /// Creates an <see cref="T:LibraryInstaller.Contracts.IProvider" /> instance and assigns the <paramref name="hostInteraction"/> to it.
        /// </summary>
        /// <param name="hostInteraction">The <see cref="T:LibraryInstaller.Contracts.IHostInteraction" /> provided by the host to handle file system writes etc.</param>
        /// <returns>A <see cref="T:LibraryInstaller.Contracts.IProvider" /> instance.</returns>
        public virtual IProvider CreateProvider(IHostInteraction hostInteraction)
        {
            return new Provider(hostInteraction);
        }
    }
}
