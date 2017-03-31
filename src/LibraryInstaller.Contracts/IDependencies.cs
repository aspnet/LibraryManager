namespace LibraryInstaller.Contracts
{
    /// <summary>
    /// Used to pass in dependencies to the Manifest class.
    /// </summary>
    /// <remarks>
    /// This should be implemented by the host
    /// </remarks>
    public interface IDependencies
    {
        /// <summary>
        /// Gets the provider based on the specified providerId.
        /// </summary>
        /// <param name="providerId">The unique ID of the provider.</param>
        /// <returns>An <see cref="IProvider"/> or <code>null</code> from the providers resolved by the host.</returns>
        IProvider GetProvider(string providerId);

        /// <summary>
        /// Gets the <see cref="IHostInteraction"/> used by <see cref="IProvider"/> to install libraries.
        /// </summary>
        /// <returns>The <see cref="IHostInteraction"/> provided by the host.</returns>
        IHostInteraction GetHostInteractions();
    }
}
