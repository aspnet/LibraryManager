using System.Collections.Generic;

namespace LibraryInstaller.Contracts
{
    /// <summary>
    /// Represents a library package
    /// </summary>
    public interface ILibrary
    {
        /// <summary>
        /// The string that lets the <see cref="IProvider"/> uniquely identify the specific library.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The unique ID of the provider.
        /// </summary>
        string ProviderId { get; }

        /// <summary>
        /// The version of the library.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// A list of files and a <code>bool</code> value to determine if the file is suggested as a default file for this library.
        /// </summary>
        IReadOnlyDictionary<string, bool> Files { get; }
    }
}