// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryManager.Contracts;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Web.LibraryManager.Mocks
{
    /// <summary>
    /// Used to pass in dependencies to the Manifest class.
    /// </summary>
    /// <seealso cref="LibraryManager.Contracts.IDependencies" />
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
            AllProviders.AddRange(factories.Select(f => f.CreateProvider(hostInteraction)));
        }

        /// <summary>
        /// Gets the set of currently registered providers
        /// </summary>
        public IReadOnlyList<IProvider> Providers => AllProviders;

        /// <summary>
        /// The collection of providers.
        /// </summary>
        public virtual List<IProvider> AllProviders { get; set; } = new List<IProvider>();

        /// <summary>
        /// Gets the <see cref="T:LibraryManager.Contracts.IHotInteraction" /> used by <see cref="T:LibraryManager.Contracts.IProvider" /> to install libraries.
        /// </summary>
        /// <returns>
        /// The <see cref="T:LibraryManager.Contracts.IHostInteraction" /> provided by the host.
        /// </returns>
        public virtual IHostInteraction GetHostInteractions() => _hostInteractions;

        /// <summary>
        /// Gets the provider based on the specified providerId.
        /// </summary>
        /// <param name="providerId">The unique ID of the provider.</param>
        /// <returns>
        /// An <see cref="T:LibraryManager.Contracts.IProvider" /> or <code>null</code> from the providers resolved by the host.
        /// </returns>
        public virtual IProvider GetProvider(string providerId)
        {
            return AllProviders.FirstOrDefault(p => p.Id == providerId);
        }
    }
}
