namespace LibraryInstaller.Contracts
{
    /// <summary>
    /// Represents a factory needed to register a <see cref="IProvider"/>.
    /// </summary>
    public interface IProviderFactory
    {
        /// <summary>
        /// Creates an <see cref="IProvider"/> instance.
        /// </summary>
        /// <param name="hostInteraction">The <see cref="IHostInteraction"/> provided by the host to handle file system writes etc.</param>
        /// <returns>A <see cref="IProvider"/> instance.</returns>
        IProvider CreateProvider(IHostInteraction hostInteraction);
    }
}
