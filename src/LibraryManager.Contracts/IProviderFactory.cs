// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Web.LibraryManager.Contracts
{
    /// <summary>
    /// Represents a factory needed to register a <see cref="IProvider"/>.
    /// </summary>
    public interface IProviderFactory
    {
        /// <summary>
        /// Creates an <see cref="IProvider"/> instance.
        /// </summary>
        /// <param name="hostInteraction">The <see cref="IHostInteraction"/> provided by the host to handle file system writes etc.</param>
        /// <returns>A <see cref="IProvider"/> instance.</returns>
        IProvider CreateProvider(IHostInteraction hostInteraction);
    }
}
