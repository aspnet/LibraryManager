using LibraryInstaller.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryInstaller.Mocks
{
    /// <summary>
    /// A mock <see cref="IProvider"/> class for use in unit tests.
    /// </summary>
    /// <seealso cref="LibraryInstaller.Contracts.IProvider" />
    public class Provider : IProvider
    {
        /// <summary>
        /// The unique identifier of the provider.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// An object specified by the host to interact with the file system etc.
        /// </summary>
        public IHostInteraction HostInteraction { get; set; }

        /// <summary>
        /// Gets or sets the catalog to return from the <see cref="GetCatalog"/> method.
        /// </summary>
        public ILibraryCatalog Catalog { get; set; }

        /// <summary>
        /// Gets or sets the result to return from the <see cref="InstallAsync"/> method.
        /// </summary>
        public ILibraryInstallationResult Result { get; set; }

        /// <summary>
        /// Gets the <see cref="T:LibraryInstaller.Contracts.ILibraryCatalog" /> for the <see cref="T:LibraryInstaller.Contracts.IProvider" />. May be <code>null</code> if no catalog is supported.
        /// </summary>
        /// <returns></returns>
        public ILibraryCatalog GetCatalog()
        {
            return Catalog;
        }

        /// <summary>
        /// Installs a library as specified in the <paramref name="desiredState" /> parameter.
        /// </summary>
        /// <param name="desiredState">The details about the library to install.</param>
        /// <param name="cancellationToken">A token that allows for the operation to be cancelled.</param>
        /// <returns>
        /// The <see cref="T:LibraryInstaller.Contracts.ILibraryInstallationResult" /> from the installation process.
        /// </returns>
        public Task<ILibraryInstallationResult> InstallAsync(ILibraryInstallationState desiredState, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result);
        }
    }
}
