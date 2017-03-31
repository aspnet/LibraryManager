using System;
using LibraryInstaller.Contracts;

namespace LibraryInstaller
{
    /// <summary>
    /// An exception to be thrown when a library is failing to install because the
    /// information in the <see cref="ILibraryInstallationState"/> is invalid.
    /// </summary>
    /// <remarks>
    /// For instance, if a <see cref="ILibraryInstallationState"/> with an id that isn't
    /// recognized by the <see cref="IProvider"/> is being passed to <see cref="Contracts.IProvider.InstallAsync"/>,
    /// this exception could be thrown so it can be handled inside <see cref="Contracts.IProvider.InstallAsync"/>
    /// and an <see cref="IError"/> added to the <see cref="ILibraryInstallationResult.Errors"/> collection.
    /// </remarks>
    public class InvalidLibraryException : Exception
    {
        /// <summary>
        /// Creates a new instance of the <see cref="InvalidLibraryException"/>.
        /// </summary>
        /// <param name="libraryId">The ID of the invalid library.</param>
        /// <param name="providerId">The ID of the <see cref="IProvider"/> failing to install the library.</param>
        public InvalidLibraryException(string libraryId, string providerId)
            : base(Resources.Text.ErrorUnableToResolveSource)
        {
            LibraryId = libraryId;
            ProviderId = providerId;
        }

        /// <summary>
        /// The ID of the invalid library
        /// </summary>
        public string LibraryId { get; }

        /// <summary>
        /// The ID of the <see cref="IProvider"/> failing to install the library.
        /// </summary>
        public string ProviderId { get; }
    }
}
