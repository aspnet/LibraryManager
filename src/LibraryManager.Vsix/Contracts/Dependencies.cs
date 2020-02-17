// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
