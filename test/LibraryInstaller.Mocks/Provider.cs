// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryInstaller.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryInstaller.Mocks
{
    /// <summary>
    /// A mock <see cref="IProvider"/> class for use in unit tests.
    /// </summary>
    /// <seealso cref="LibraryInstaller.Contracts.IProvider" />
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
        public virtual ILibraryInstallationResult Result { get; set; }

        /// <summary>
        /// Gets the <see cref="T:LibraryInstaller.Contracts.ILibraryCatalog" /> for the <see cref="T:LibraryInstaller.Contracts.IProvider" />. May be <code>null</code> if no catalog is supported.
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
        /// The <see cref="T:LibraryInstaller.Contracts.ILibraryInstallationResult" /> from the installation process.
        /// </returns>
        public virtual Task<ILibraryInstallationResult> InstallAsync(ILibraryInstallationState desiredState, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result);
        }
    }
}
