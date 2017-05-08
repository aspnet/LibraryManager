// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryInstaller.Contracts;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Web.LibraryInstaller
{

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <seealso cref="Microsoft.Web.LibraryInstaller.Contracts.ILibraryInstallationState" />
    public class LibraryInstallationState : ILibraryInstallationState
    {
        /// <summary>
        /// The unique identifier of the provider.
        /// </summary>
        [JsonProperty("provider")]
        public string ProviderId { get; set; }

        /// <summary>
        /// The identifyer to uniquely identify the library
        /// </summary>
        [JsonProperty("id")]
        public string LibraryId { get; set; }

        /// <summary>
        /// The path relative to the working directory to copy the files to.
        /// </summary>
        [JsonProperty("path")]
        public string DestinationPath { get; set; }

        /// <summary>
        /// The list of file names to install
        /// </summary>
        [JsonProperty("files")]
        public IReadOnlyList<string> Files { get; set; }

        /// <summary>Internal use only</summary>
        public static LibraryInstallationState FromInterface(ILibraryInstallationState state, string defaultProviderId = null)
        {
            string normalizedProviderId = state.ProviderId ?? defaultProviderId;

            return new LibraryInstallationState
            {
                LibraryId = state.LibraryId,
                ProviderId = normalizedProviderId,
                Files = state.Files,
                DestinationPath = state.DestinationPath
            };
        }
    }
}
