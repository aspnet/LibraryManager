// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.LibraryNaming;
using Microsoft.Web.LibraryManager.Vsix.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal class Dependencies : IDependencies
    {
        private readonly IHostInteraction _hostInteraction;
        private readonly List<IProvider> _providers = new List<IProvider>();
        private static readonly Dictionary<string, Dependencies> _cache = new Dictionary<string, Dependencies>();

        [ImportMany(typeof(IProviderFactory), AllowRecomposition = true)]
        private IEnumerable<IProviderFactory> _providerFactories = null;

        private Dependencies(IHostInteraction hostInteraction)
        {
            _hostInteraction = hostInteraction;
            Initialize();
        }

        public IReadOnlyList<IProvider> Providers => _providers;

        public IHostInteraction GetHostInteractions() => _hostInteraction;

        public static Dependencies FromConfigFile(string configFilePath)
        {
            if (!_cache.ContainsKey(configFilePath))
            {
                PerProjectLogger perProjectLogger = new PerProjectLogger(configFilePath);
                HostInteraction hostInteraction = new HostInteraction(configFilePath, perProjectLogger);
                _cache[configFilePath] = new Dependencies(hostInteraction);
            }

            // We need to initialize naming schemes for the providers before we can proceed with any operation.
            LibraryIdToNameAndVersionConverter.Instance.EnsureInitialized(_cache[configFilePath]);

            return _cache[configFilePath];
        }

        public IProvider GetProvider(string providerId)
        {
            return Providers?.FirstOrDefault(p => p.Id.Equals(providerId, StringComparison.OrdinalIgnoreCase));
        }

        private void Initialize()
        {
            if (_providers.Count > 0)
            {
                return;
            }

            this.SatisfyImportsOnce();

            foreach (IProviderFactory factory in _providerFactories)
            {
                IProvider provider = factory.CreateProvider(_hostInteraction);

                if (provider != null && !_providers.Contains(provider))
                {
                    _providers.Add(provider);
                }
            }
        }
    }
}
