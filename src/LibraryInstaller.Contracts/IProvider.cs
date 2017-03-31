using System.Threading;
using System.Threading.Tasks;

namespace LibraryInstaller.Contracts
{
    /// <summary>
    /// Represents a library provider.
    /// </summary>
    public interface IProvider
    {
        /// <summary>
        /// The unique identifier of the provider.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// An object specified by the host to interact with the file system etc.
        /// </summary>
        IHostInteraction HostInteraction { get; set; }

        /// <summary>
        /// Installs a library as specified in the <paramref name="desiredState"/> parameter.
        /// </summary>
        /// <param name="desiredState">The details about the library to install.</param>
        /// <param name="cancellationToken">A token that allows for the operation to be cancelled.</param>
        /// <returns>The <see cref="ILibraryInstallationResult"/> from the installation process.</returns>
        Task<ILibraryInstallationResult> InstallAsync(ILibraryInstallationState desiredState, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the <see cref="ILibraryCatalog"/> for the <see cref="IProvider"/>. May be <code>null</code> if no catalog is supported.
        /// </summary>
        /// <returns></returns>
        ILibraryCatalog GetCatalog();
    }
}
