// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Web.LibraryManager.Contracts
{
    /// <summary>
    /// Used to pass in dependencies to the Manifest class.
    /// </summary>
    /// <remarks>
    /// This should be implemented by the host
    /// </remarks>
    public interface IDependencies
    {
        /// <summary>
        /// Gets the set of providers that are currently registered
        /// </summary>
        IReadOnlyList<IProvider> Providers { get; }

        /// <summary>
        /// Gets the provider based on the specified providerId.
        /// </summary>
        /// <param name="providerId">The unique ID of the provider.</param>
        /// <returns>An <see cref="IProvider"/> or <code>null</code> from the providers resolved by the host.</returns>
        IProvider GetProvider(string providerId);

        /// <summary>
        /// Gets the <see cref="IHostInteraction"/> used by <see cref="IProvider"/> to install libraries.
        /// </summary>
        /// <returns>The <see cref="IHostInteraction"/> provided by the host.</returns>
        IHostInteraction GetHostInteractions();
    }
}
