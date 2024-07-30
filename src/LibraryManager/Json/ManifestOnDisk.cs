// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.Web.LibraryManager.Json
{
    internal class ManifestOnDisk
    {
        [JsonProperty(ManifestConstants.Version)]
        public string Version { get; set; }

        [JsonProperty(ManifestConstants.DefaultProvider)]
        public string DefaultProvider { get; set; }

        [JsonProperty(ManifestConstants.DefaultDestination)]
        public string DefaultDestination { get; set; }

        [JsonProperty(ManifestConstants.Libraries)]
        public IEnumerable<LibraryInstallationStateOnDisk> Libraries { get; set; }
    }
}
