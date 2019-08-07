// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Tools.Contracts
{
    /// <inheritdoc />
    internal class Dependencies : IDependencies
    {
        private readonly IHostInteraction _hostInteraction;
        private readonly List<IProvider> _providers = new List<IProvider>();

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

            // Prepare part discovery to support both flavors of MEF attributes.
            PartDiscovery discovery = PartDiscovery.Combine(
                new AttributedPartDiscovery(Resolver.DefaultInstance), // "NuGet MEF" attributes (Microsoft.Composition)
                new AttributedPartDiscoveryV1(Resolver.DefaultInstance)); // ".NET MEF" attributes (System.ComponentModel.Composition)

            Task<DiscoveredParts> t = discovery.CreatePartsAsync(_assemblyPaths);
            t.Wait();

            // Build up a catalog of MEF parts
            ComposableCatalog catalog = ComposableCatalog.Create(Resolver.DefaultInstance).AddParts(t.Result);

            // Assemble the parts into a valid graph.
            CompositionConfiguration config = CompositionConfiguration.Create(catalog);

            // Prepare an ExportProvider factory based on this graph.
            IExportProviderFactory epf = config.CreateExportProviderFactory();

            // Create an export provider, which represents a unique container of values.
            // You can create as many of these as you want, but typically an app needs just one.
            ExportProvider vsExportProvider = epf.CreateExportProvider();

            // Obtain .NET shim for the export provider to keep the rest of the editors code the same
            System.ComponentModel.Composition.Hosting.ExportProvider exportProvider = vsExportProvider.AsExportProvider();

            IEnumerable<IProviderFactory> providerFactories = exportProvider.GetExportedValues<IProviderFactory>();

            foreach (IProviderFactory factory in providerFactories)
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
