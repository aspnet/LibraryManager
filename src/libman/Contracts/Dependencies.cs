// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Tools.Contracts
{
    /// <inheritdoc />
    internal class Dependencies : IDependencies
    {
        private readonly IHostInteraction _hostInteraction;
        private readonly List<IProvider> _providers = new List<IProvider>();

        [ImportMany]
        private IEnumerable<IProviderFactory> _providerFactories;

        private readonly IEnumerable<string> _assemblyPaths;

        public Dependencies(IHostEnvironment environment)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            _hostInteraction = environment?.HostInteraction;

            string toolInstallationDir = environment.ToolInstallationDir;
            //TODO: This will scan all dependencies of the tool.
            // Need to figure out how to handle when extensions are installed.
            _assemblyPaths = Directory.EnumerateFiles(toolInstallationDir, "*.dll");

            Initialize();
        }

        /// <inheritdoc />
        public IHostInteraction GetHostInteractions() => _hostInteraction;

        /// <inheritdoc />
        public IReadOnlyList<IProvider> Providers => _providers;

        /// <inheritdoc />
        public IProvider GetProvider(string providerId)
        {
            return _providers?.FirstOrDefault(p => p.Id.Equals(providerId, StringComparison.OrdinalIgnoreCase));
        }

        private void Initialize()
        {
            if (_providers.Count > 0)
                return;

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
                    var provider = factory.CreateProvider(_hostInteraction);
                    if (!string.IsNullOrEmpty(provider.Id))
                    {
                        _providers.Add(provider);
                    }
                }
            }
        }
    }
}
