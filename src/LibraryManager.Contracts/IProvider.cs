// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
        /// <returns>The <see cref="OperationResult{LibraryInstallationGoalState}"/> from the installation process.</returns>
        Task<OperationResult<LibraryInstallationGoalState>> InstallAsync(ILibraryInstallationState desiredState, CancellationToken cancellationToken);

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

        /// <summary>
        /// Gets the goal state of the library installation.  Does not imply actual installation.
        /// </summary>
        Task<OperationResult<LibraryInstallationGoalState>> GetInstallationGoalStateAsync(ILibraryInstallationState installationState, CancellationToken cancellationToken);
    }
}
