// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
