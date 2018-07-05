// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryManager.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Mocks
{
    /// <summary>
    /// A mock <see cref="IProvider"/> class for use in unit tests.
    /// </summary>
    /// <seealso cref="LibraryManager.Contracts.IProvider" />
    public class Provider : IProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Provider"/> class.
        /// </summary>
        /// <param name="hostInteraction">The host interaction.</param>
        public Provider(IHostInteraction hostInteraction)
        {
            HostInteraction = hostInteraction;
        }

        /// <summary>
        /// The unique identifier of the provider.
        /// </summary>
        public virtual string Id { get; set; }

        /// <summary>
        /// The NuGet Package id for the package including the provider for use by MSBuild.
        /// </summary>
        /// <remarks>
        /// If the provider doesn't have a NuGet package, then return <code>null</code>.
        /// </remarks>
        public string NuGetPackageId { get; set; }

        /// <summary>
        /// Hint text for library id.
        /// </summary>
        /// <remarks>
        /// If the provider doesn't have a hint text, then return empty string.
        /// </remarks>
        public string LibraryIdHintText { get; set; }

        /// <summary>
        /// An object specified by the host to interact with the file system etc.
        /// </summary>
        public virtual IHostInteraction HostInteraction { get; set; }

        /// <summary>
        /// Gets or sets the catalog to return from the <see cref="GetCatalog"/> method.
        /// </summary>
        public virtual ILibraryCatalog Catalog { get; set; }

        /// <summary>
        /// Gets or sets the result to return from the <see cref="InstallAsync"/> method.
        /// </summary>
        public virtual ILibraryOperationResult Result { get; set; }

        /// <summary>
        /// Indicates whether libraries with versions are supported.
        /// </summary>
        public bool SupportsLibraryVersions { get; set; }

        /// <summary>
        /// Gets the <see cref="T:LibraryManager.Contracts.ILibraryCatalog" /> for the <see cref="T:LibraryManager.Contracts.IProvider" />. May be <code>null</code> if no catalog is supported.
        /// </summary>
        /// <returns></returns>
        public virtual ILibraryCatalog GetCatalog()
        {
            return Catalog;
        }

        /// <summary>
        /// Installs a library as specified in the <paramref name="desiredState" /> parameter.
        /// </summary>
        /// <param name="desiredState">The details about the library to install.</param>
        /// <param name="cancellationToken">A token that allows for the operation to be cancelled.</param>
        /// <returns>
        /// The <see cref="T:LibraryManager.Contracts.ILibraryOperationResult" /> from the installation process.
        /// </returns>
        public virtual Task<ILibraryOperationResult> InstallAsync(ILibraryInstallationState desiredState, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result);
        }

        /// <summary>
        /// No-op
        /// </summary>
        /// <param name="desiredState"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task<ILibraryOperationResult> UpdateStateAsync(ILibraryInstallationState desiredState, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="library"></param>
        /// <returns></returns>
        public string GetSuggestedDestination(ILibrary library)
        {
            return library?.Name;
        }
    }
}
