namespace LibraryInstaller.Contracts
{
    /// <summary>
    /// A object returned from <see cref="IProvider.InstallAsync"/> method in case of any errors occured during installation.
    /// </summary>
    public interface IError
    {
        /// <summary>
        /// The error code used to uniquely identify the error.
        /// </summary>
        string Code { get; }

        /// <summary>
        /// The user friendly description of the error.
        /// </summary>
        string Message { get; }
    }
}