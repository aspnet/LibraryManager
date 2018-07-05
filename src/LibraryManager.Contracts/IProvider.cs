// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Contracts
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
        /// The NuGet Package id for the package including the provider for use by MSBuild.
        /// </summary>
        /// <remarks>
        /// If the provider doesn't have a NuGet package, then return <code>null</code>.
        /// </remarks>
        string NuGetPackageId { get; }

        /// <summary>
        /// Hint text for the library id.
        /// </summary>
        string LibraryIdHintText { get; }

        /// <summary>
        /// An object specified by the host to interact with the file system etc.
        /// </summary>
        IHostInteraction HostInteraction { get; }

        /// <summary>
        /// Indicates whether the provider supports libraries with versions.
        /// </summary>
        bool SupportsLibraryVersions { get; }

        /// <summary>
        /// Installs a library as specified in the <paramref name="desiredState"/> parameter.
        /// </summary>
        /// <param name="desiredState">The details about the library to install.</param>
        /// <param name="cancellationToken">A token that allows for the operation to be cancelled.</param>
        /// <returns>The <see cref="ILibraryOperationResult"/> from the installation process.</returns>
        Task<ILibraryOperationResult> InstallAsync(ILibraryInstallationState desiredState, CancellationToken cancellationToken);

        /// <summary>
        /// Updates library state using catalog if needed
        /// </summary>
        /// <param name="desiredState"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ILibraryOperationResult> UpdateStateAsync(ILibraryInstallationState desiredState, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the <see cref="ILibraryCatalog"/> for the <see cref="IProvider"/>. May be <code>null</code> if no catalog is supported.
        /// </summary>
        /// <returns></returns>
        ILibraryCatalog GetCatalog();

        /// <summary>
        /// Gets the suggested destination path for the library.
        /// </summary>
        /// <param name="library"></param>
        string GetSuggestedDestination(ILibrary library);
    }
}
