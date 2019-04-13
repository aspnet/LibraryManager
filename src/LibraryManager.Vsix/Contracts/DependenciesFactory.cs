using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.LibraryNaming;

namespace Microsoft.Web.LibraryManager.Vsix.Contracts
{
    /// <summary>
    /// Factory for creating Dependencies in this host
    /// </summary>
    [Export(typeof(IDependenciesFactory))]
    internal class DependenciesFactory : IDependenciesFactory
    {
        [ImportMany(typeof(IProviderFactory), AllowRecomposition = true)]
        private IEnumerable<IProviderFactory> _providerFactories = null;

        private static readonly Dictionary<string, Dependencies> _cache = new Dictionary<string, Dependencies>();

        /// <summary>
        /// Creates or re-uses a cached Dependencies for the given path
        /// </summary>
        /// <param name="configFilePath">File path to the libman.json file</param>
        public IDependencies FromConfigFile(string configFilePath)
        {
            if (!_cache.ContainsKey(configFilePath))
            {
                var perProjectLogger = new PerProjectLogger(configFilePath);
                var hostInteraction = new HostInteraction(configFilePath, perProjectLogger);
                IReadOnlyList<IProvider> providers = GetProviders(hostInteraction);
                _cache[configFilePath] = new Dependencies(hostInteraction, providers);
            }

            // We need to initialize naming schemes for the providers before we can proceed with any operation.
            LibraryIdToNameAndVersionConverter.Instance.EnsureInitialized(_cache[configFilePath]);

            return _cache[configFilePath];
        }

        private IReadOnlyList<IProvider> GetProviders(IHostInteraction hostInteraction)
        {
            return _providerFactories.Select(pf => pf.CreateProvider(hostInteraction)).ToList();
        }
    }
}
