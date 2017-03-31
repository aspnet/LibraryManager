// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using LibraryInstaller.Contracts;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace LibraryInstaller
{
    internal class LibraryInstallationState : ILibraryInstallationState
    {
        [JsonProperty("provider")]
        public string ProviderId { get; set; }

        [JsonProperty("id")]
        public string LibraryId { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("files")]
        public IReadOnlyList<string> Files { get; set; }

        public static LibraryInstallationState FromInterface(ILibraryInstallationState state)
        {
            return new LibraryInstallationState
            {
                LibraryId = state.LibraryId,
                ProviderId = state.ProviderId,
                Files = state.Files,
                Path = state.Path
            };
        }
    }
}
