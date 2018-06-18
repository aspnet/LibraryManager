// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Helpers;
using Newtonsoft.Json;

namespace Microsoft.Web.LibraryManager
{

    /// <inheritdoc />
    /// <seealso cref="Microsoft.Web.LibraryManager.Contracts.ILibraryInstallationState" />
    internal class LibraryInstallationState : ILibraryInstallationState
    {
        /// <summary>
        /// The unique identifier of the provider.
        /// </summary>
        [JsonProperty(ManifestConstants.Provider)]
        public string ProviderId { get; set; }

        /// <summary>
        /// The identifyer to uniquely identify the library
        /// </summary>
        [JsonProperty(ManifestConstants.Library)]
        public string LibraryId
        {
             get
             {
                return LibraryNamingScheme.Instance.GetLibraryId(Name, Version);
             }
             set
             {
                (string Name, string Version) nameAndVersion = LibraryNamingScheme.Instance.GetLibraryNameAndVersion(
                    value);

                Name = nameAndVersion.Name;
                Version = nameAndVersion.Version;
             }
        }

        /// <summary>
        /// The path relative to the working directory to copy the files to.
        /// </summary>
        [JsonProperty(ManifestConstants.Destination)]
        public string DestinationPath { get; set; }

        /// <summary>
        /// The list of file names to install
        /// </summary>
        [JsonProperty(ManifestConstants.Files)]
        public IReadOnlyList<string> Files { get; set; }

        /// <summary>
        /// Tells whether the library was installed/ restored using default provider.
        /// </summary>
        [JsonIgnore]
        public bool IsUsingDefaultProvider { get; set; }

        /// <summary>
        /// Tells whether the library was installed/ restored to default destination.
        /// </summary>
        [JsonIgnore]
        public bool IsUsingDefaultDestination { get; set; }

        /// <summary>
        /// Name of the library.
        /// </summary>
        [JsonIgnore]
        public string Name { get; set; }

        /// <summary>
        /// Version of the library.
        /// </summary>
        [JsonIgnore]
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
                LibraryId = state.LibraryId,
                ProviderId = normalizedProviderId,
                Files = state.Files,
                DestinationPath = normalizedDestinationPath
            };
        }
    }
}
