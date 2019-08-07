// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Build
{
    internal class Dependencies : IDependencies
    {
        private readonly IHostInteraction _hostInteraction;
        private readonly List<IProvider> _providers = new List<IProvider>();
        private readonly IEnumerable<string> _assemblyPaths;

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
                    _providers.Add(factory.CreateProvider(_hostInteraction));
                }
            }
        }
    }
}
