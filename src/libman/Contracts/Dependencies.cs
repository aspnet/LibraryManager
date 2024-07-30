// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Web.LibraryManager.Cache;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Providers.Cdnjs;
using Microsoft.Web.LibraryManager.Providers.FileSystem;
using Microsoft.Web.LibraryManager.Providers.jsDelivr;
using Microsoft.Web.LibraryManager.Providers.Unpkg;

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

            var packageInfoFactory = new NpmPackageInfoFactory(WebRequestHandler.Instance);
            var packageSearch = new NpmPackageSearch(WebRequestHandler.Instance);

            IEnumerable<IProviderFactory> factories = new IProviderFactory[] {
                new UnpkgProviderFactory(packageSearch, packageInfoFactory),
                new JsDelivrProviderFactory(packageSearch, packageInfoFactory),
                new FileSystemProviderFactory(),
                new CdnjsProviderFactory(),
            };

            foreach (IProviderFactory factory in factories)
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
