using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.Web.LibraryManager.Cache;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.LibraryNaming;
using Microsoft.Web.LibraryManager.Providers.Cdnjs;
using Microsoft.Web.LibraryManager.Providers.FileSystem;
using Microsoft.Web.LibraryManager.Providers.jsDelivr;
using Microsoft.Web.LibraryManager.Providers.Unpkg;

namespace Microsoft.Web.LibraryManager.Vsix.Contracts
{
    /// <summary>
    /// Factory for creating Dependencies in this host
    /// </summary>
    [Export(typeof(IDependenciesFactory))]
    internal class DependenciesFactory : IDependenciesFactory
    {
        private IEnumerable<IProviderFactory> ProviderFactories { get; set; }

        private static Dictionary<string, Dependencies> Cache { get; } = new Dictionary<string, Dependencies>();

        [ImportingConstructor]
        public DependenciesFactory()
        {
            var packageSearch = new NpmPackageSearch(WebRequestHandler.Instance);
            var packageInfoFactory = new NpmPackageInfoFactory(WebRequestHandler.Instance);

            ProviderFactories = new IProviderFactory[] {
                new FileSystemProviderFactory(),
                new CdnjsProviderFactory(),
                new UnpkgProviderFactory(packageSearch, packageInfoFactory),
                new JsDelivrProviderFactory(packageSearch, packageInfoFactory),
            };
        }

        /// <summary>
        /// Creates or re-uses a cached Dependencies for the given path
        /// </summary>
        /// <param name="configFilePath">File path to the libman.json file</param>
        public IDependencies FromConfigFile(string configFilePath)
        {
            if (!Cache.ContainsKey(configFilePath))
            {
                var perProjectLogger = new PerProjectLogger(configFilePath);
                var hostInteraction = new HostInteraction(configFilePath, perProjectLogger);
                IReadOnlyList<IProvider> providers = GetProviders(hostInteraction);
                Cache[configFilePath] = new Dependencies(hostInteraction, providers);
            }

            // We need to initialize naming schemes for the providers before we can proceed with any operation.
            LibraryIdToNameAndVersionConverter.Instance.EnsureInitialized(Cache[configFilePath]);

            return Cache[configFilePath];
        }

        private IReadOnlyList<IProvider> GetProviders(IHostInteraction hostInteraction)
        {
            return ProviderFactories.Select(pf => pf.CreateProvider(hostInteraction)).ToList();
        }
    }
}
