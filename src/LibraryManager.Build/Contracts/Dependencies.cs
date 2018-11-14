// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryManager.Contracts;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.IO;

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
#if NET472
                assembly = Assembly.LoadFrom(path);
#else
                assembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(path);

#endif

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
