// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryInstaller.Contracts;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Web.LibraryInstaller.Build
{
    public class Dependencies : IDependencies
    {
        private IHostInteraction _hostInteraction;
        private static List<IProvider> _providers = new List<IProvider>();
        private static Dictionary<string, Dependencies> _cache = new Dictionary<string, Dependencies>();
        private IEnumerable<string> _assemblyPaths;

        private Dependencies(IHostInteraction hostInteraction, IEnumerable<string> assemblyPaths)
        {
            _hostInteraction = hostInteraction;
            _assemblyPaths = assemblyPaths;
            Initialize();
        }

        public IHostInteraction GetHostInteractions() => _hostInteraction;

        public static Dependencies FromTask(RestoreTask task, IEnumerable<string> assemblyPaths)
        {
            if (!_cache.ContainsKey(task.FileName))
            {
                var hostInteraction = new HostInteraction(task);
                _cache[task.FileName] = new Dependencies(hostInteraction, assemblyPaths);
            }

            return _cache[task.FileName];
        }

        public IProvider GetProvider(string providerId)
        {
            return _providers?.FirstOrDefault(p => p.Id.Equals(providerId, StringComparison.OrdinalIgnoreCase));
        }

        private void Initialize()
        {
            if (_providers.Any())
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
#if NET46
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
