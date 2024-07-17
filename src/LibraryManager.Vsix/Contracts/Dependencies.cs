// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix.Contracts
{
    internal class Dependencies : IDependencies
    {
        private readonly IHostInteraction _hostInteraction;

        internal Dependencies(IHostInteraction hostInteraction, IReadOnlyList<IProvider> providers)
        {
            _hostInteraction = hostInteraction;
            Providers = providers;
        }

        public IReadOnlyList<IProvider> Providers { get; }

        public IHostInteraction GetHostInteractions() => _hostInteraction;

        public IProvider GetProvider(string providerId)
        {
            return Providers?.FirstOrDefault(p => p.Id.Equals(providerId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
