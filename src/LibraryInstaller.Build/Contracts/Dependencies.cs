// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryInstaller.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Web.LibraryInstaller.Providers.Cdnjs;
using Microsoft.Web.LibraryInstaller.Providers.FileSystem;

namespace Microsoft.Web.LibraryInstaller.Build
{
    public class Dependencies : IDependencies
    {
        private IHostInteraction _hostInteraction;
        private static List<IProvider> _providers = new List<IProvider>();
        private static Dictionary<string, Dependencies> _cache = new Dictionary<string, Dependencies>();

        private Dependencies(IHostInteraction hostInteraction)
        {
            _hostInteraction = hostInteraction;
            Initialize();
        }

        public IHostInteraction GetHostInteractions() => _hostInteraction;

        public static Dependencies FromTask(RestoreTask task)
        {
            if (!_cache.ContainsKey(task.FileName))
            {
                var hostInteraction = new HostInteraction(task);
                _cache[task.FileName] = new Dependencies(hostInteraction);
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

            IProviderFactory[] factories = { new CdnjsProviderFactory(), new FileSystemProviderFactory() };

            foreach (IProviderFactory factory in factories)
            {
                _providers.Add(factory.CreateProvider(_hostInteraction));
            }
        }
    }
}
