// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Mocks
{
    /// <summary>
    /// A mock <see cref="IProviderFactory"/> class for use in unit tests.
    /// </summary>
    /// <seealso cref="LibraryManager.Contracts.IProviderFactory" />
    public class ProviderFactory : IProviderFactory
    {
        /// <summary>
        /// Creates an <see cref="T:LibraryManager.Contracts.IProvider" /> instance and assigns the <paramref name="hostInteraction"/> to it.
        /// </summary>
        /// <param name="hostInteraction">The <see cref="T:LibraryManager.Contracts.IHostInteraction" /> provided by the host to handle file system writes etc.</param>
        /// <returns>A <see cref="T:LibraryManager.Contracts.IProvider" /> instance.</returns>
        public virtual IProvider CreateProvider(IHostInteraction hostInteraction)
        {
            return new Provider(hostInteraction);
        }
    }
}
