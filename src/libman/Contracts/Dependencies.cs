// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

            IEnumerable<IProviderFactory> factories = GetProvidersFromReflection();

            foreach (IProviderFactory factory in factories)
            {
                if (factory != null)
                {
                    _providers.Add(factory.CreateProvider(_hostInteraction));
                }
            }
        }

        private IEnumerable<IProviderFactory> GetProvidersFromReflection()
        {
            var list = new List<IProviderFactory>();

            foreach (string path in _assemblyPaths)
            {
                Assembly assembly;
                assembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(path);

                IEnumerable<IProviderFactory> factories = assembly
                    .DefinedTypes
                    .Where(p => p.ImplementedInterfaces.Any(i => i.FullName == typeof(IProviderFactory).FullName))
                    .Select(fac => Activator.CreateInstance(assembly.GetType(fac.FullName)) as IProviderFactory);

                list.AddRange(factories);
            }

            return list;
        }
    }
}
