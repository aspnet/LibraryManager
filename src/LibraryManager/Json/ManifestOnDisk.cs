// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
