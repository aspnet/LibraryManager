// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
