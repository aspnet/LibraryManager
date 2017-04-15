// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryInstaller.Contracts;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Web.LibraryInstaller.Mocks
{
    /// <summary>
    /// Used to pass in dependencies to the Manifest class.
    /// </summary>
    /// <seealso cref="LibraryInstaller.Contracts.IDependencies" />
    /// <remarks>
    /// This should be implemented by the host
    /// </remarks>
    public class Dependencies : IDependencies
    {
        private IHostInteraction _hostInteractions;

        /// <summary>
        /// Initializes a new instance of the <see cref="Dependencies"/> class.
        /// </summary>
        /// <param name="hostInteraction">The host interactions.</param>
        /// <param name="factories">The provider factories.</param>
        public Dependencies(IHostInteraction hostInteraction, params IProviderFactory[] factories)
        {
            _hostInteractions = hostInteraction;
            Providers.AddRange(factories.Select(f => f.CreateProvider(hostInteraction)));
        }


        /// <summary>
        /// The collection of providers.
        /// </summary>
        public virtual List<IProvider> Providers { get; set; } = new List<IProvider>();

        /// <summary>
        /// Gets the <see cref="T:LibraryInstaller.Contracts.IHotInteraction" /> used by <see cref="T:LibraryInstaller.Contracts.IProvider" /> to install libraries.
        /// </summary>
        /// <returns>
        /// The <see cref="T:LibraryInstaller.Contracts.IHostInteraction" /> provided by the host.
        /// </returns>
        public virtual IHostInteraction GetHostInteractions() => _hostInteractions;

        /// <summary>
        /// Gets the provider based on the specified providerId.
        /// </summary>
        /// <param name="providerId">The unique ID of the provider.</param>
        /// <returns>
        /// An <see cref="T:LibraryInstaller.Contracts.IProvider" /> or <code>null</code> from the providers resolved by the host.
        /// </returns>
        public virtual IProvider GetProvider(string providerId)
        {
            return Providers.FirstOrDefault(p => p.Id == providerId);
        }
    }
}
