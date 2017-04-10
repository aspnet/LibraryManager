// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryInstaller.Contracts;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Web.LibraryInstaller
{
    internal class LibraryInstallationState : ILibraryInstallationState
    {
        [JsonProperty("provider")]
        public string ProviderId { get; set; }

        [JsonProperty("id")]
        public string LibraryId { get; set; }

        [JsonProperty("path")]
        public string DestinationPath { get; set; }

        [JsonProperty("files")]
        public IReadOnlyList<string> Files { get; set; }

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
