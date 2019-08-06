// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Build
{
    internal class Dependencies : IDependencies
    {
        private readonly IHostInteraction _hostInteraction;
        private readonly List<IProvider> _providers = new List<IProvider>();
        private readonly IEnumerable<string> _assemblyPaths;

        [ImportMany]
        private IEnumerable<IProviderFactory> _providerFactories;

        private Dependencies(IHostInteraction hostInteraction, IEnumerable<string> assemblyPaths)
        {
            _hostInteraction = hostInteraction;
            _assemblyPaths = assemblyPaths;
            Initialize();
        }

        public IHostInteraction GetHostInteractions() => _hostInteraction;

        public static Dependencies FromTask(string workingDirectory, IEnumerable<string> assemblyPaths)
        {
            var hostInteraction = new HostInteraction(workingDirectory);
            return new Dependencies(hostInteraction, assemblyPaths);
        }

        public IReadOnlyList<IProvider> Providers => _providers;

        public IProvider GetProvider(string providerId)
        {
            return _providers?.FirstOrDefault(p => p.Id.Equals(providerId, StringComparison.OrdinalIgnoreCase));
        }

        private void Initialize()
        {
            AggregateCatalog aggregateCatalog = new AggregateCatalog();

            foreach (string assemblyPath in _assemblyPaths)
            {
                Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
                aggregateCatalog.Catalogs.Add(new AssemblyCatalog(assembly));
            }

            CompositionContainer container = new CompositionContainer(aggregateCatalog);
            container.SatisfyImportsOnce(this);

            foreach (IProviderFactory factory in _providerFactories)
            {
                if (factory != null)
                {
                    _providers.Add(factory.CreateProvider(_hostInteraction));
                }
            }
        }
    }
}
