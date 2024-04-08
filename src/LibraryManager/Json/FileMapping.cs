// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Newtonsoft.Json;

#nullable enable

namespace Microsoft.Web.LibraryManager.Json
{
    internal class FileMapping
    {
        [JsonProperty(ManifestConstants.Root)]
        public string? Root { get; set; }

        [JsonProperty(ManifestConstants.Destination)]
        public string? Destination { get; set; }

        [JsonProperty(ManifestConstants.Files)]
        public IReadOnlyList<string>? Files { get; set; }
    }
}
