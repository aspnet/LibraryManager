// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager
{

    /// <inheritdoc />
    /// <seealso cref="Microsoft.Web.LibraryManager.Contracts.ILibraryInstallationState" />
    internal class LibraryInstallationState : ILibraryInstallationState
    {
        /// <summary>
        /// The unique identifier of the provider.
        /// </summary>
        public string ProviderId { get; set; }

        /// <summary>
        /// The path relative to the working directory to copy the files to.
        /// </summary>
        public string DestinationPath { get; set; }

        /// <summary>
        /// The list of file names to install
        /// </summary>
        public IReadOnlyList<string> Files { get; set; }

        /// <summary>
        /// Tells whether the library was installed/ restored using default provider.
        /// </summary>
        public bool IsUsingDefaultProvider { get; set; }

        /// <summary>
        /// Tells whether the library was installed/ restored to default destination.
        /// </summary>
        public bool IsUsingDefaultDestination { get; set; }

        /// <summary>
        /// Name of the library.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Version of the library.
        /// </summary>
        public string Version { get; set; }

        /// <summary>Internal use only</summary>
        public static LibraryInstallationState FromInterface(ILibraryInstallationState state,
                                                             string defaultProviderId = null,
                                                             string defaultDestination = null)
        {
            string normalizedProviderId = state.ProviderId ?? defaultProviderId;
            string normalizedDestinationPath = state.DestinationPath ?? defaultDestination;

            return new LibraryInstallationState
            {
                Name = state.Name,
                Version = state.Version,
                ProviderId = normalizedProviderId,
                Files = state.Files,
                DestinationPath = normalizedDestinationPath,
                IsUsingDefaultDestination = state.IsUsingDefaultDestination,
                IsUsingDefaultProvider = state.IsUsingDefaultProvider
            };
        }
    }
}
