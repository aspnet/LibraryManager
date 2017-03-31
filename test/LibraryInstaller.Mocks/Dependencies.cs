using LibraryInstaller.Contracts;
using System.Collections.Generic;
using System.Linq;

namespace LibraryInstaller.Mocks
{
    /// <summary>
    /// Used to pass in dependencies to the Manifest class.
    /// </summary>
    /// <seealso cref="LibraryInstaller.Contracts.IDependencies" />
    /// <remarks>
    /// This should be implemented by the host
    /// </remarks>
    public class Dependencies : IDependencies
    {
        private IHostInteraction _hostInteractions;

        /// <summary>
        /// Initializes a new instance of the <see cref="Dependencies"/> class.
        /// </summary>
        /// <param name="hostInteractions">The host interactions.</param>
        public Dependencies(IHostInteraction hostInteractions)
        {
            _hostInteractions = hostInteractions;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Dependencies"/> class.
        /// </summary>
        /// <param name="hostInteractions">The host interactions.</param>
        /// <param name="providers">The providers.</param>
        public Dependencies(IHostInteraction hostInteractions, params IProvider[] providers)
        {
            _hostInteractions = hostInteractions;
            Providers.AddRange(providers);
        }


        /// <summary>
        /// The collection of providers.
        /// </summary>
        public List<IProvider> Providers { get; set; } = new List<IProvider>();

        /// <summary>
        /// Gets the <see cref="T:LibraryInstaller.Contracts.IHostInteraction" /> used by <see cref="T:LibraryInstaller.Contracts.IProvider" /> to install libraries.
        /// </summary>
        /// <returns>
        /// The <see cref="T:LibraryInstaller.Contracts.IHostInteraction" /> provided by the host.
        /// </returns>
        public IHostInteraction GetHostInteractions() => _hostInteractions;

        /// <summary>
        /// Gets the provider based on the specified providerId.
        /// </summary>
        /// <param name="providerId">The unique ID of the provider.</param>
        /// <returns>
        /// An <see cref="T:LibraryInstaller.Contracts.IProvider" /> or <code>null</code> from the providers resolved by the host.
        /// </returns>
        public IProvider GetProvider(string providerId)
        {
            IProvider provider = Providers.FirstOrDefault(p => p.Id == providerId);

            if (provider != null)
            {
                provider.HostInteraction = _hostInteractions;
            }

            return provider;
        }
    }
}
