// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.Web.LibraryManager.Json
{
    internal class LibraryInstallationStateOnDisk
    {
        [JsonProperty(ManifestConstants.Provider)]
        public string ProviderId { get; set; }

        [JsonProperty(ManifestConstants.Library)]
        public string LibraryId { get; set; }

        [JsonProperty(ManifestConstants.Destination)]
        public string DestinationPath { get; set; }

        [JsonProperty(ManifestConstants.Files)]
        public IReadOnlyList<string> Files { get; set; }
    }
}
