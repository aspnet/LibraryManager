// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Web.LibraryManager.Contracts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Web.LibraryManager.Providers.Cdnjs
{
    internal class CdnjsLibraryGroup : ILibraryGroup
    {
        [JsonProperty("name")]
        public string DisplayName { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        public Task<IEnumerable<string>> GetLibraryVersions(CancellationToken cancellationToken)
        {
            return DisplayInfosTask?.Invoke(cancellationToken) ?? Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
        }

        public Func<CancellationToken, Task<IEnumerable<string>>> DisplayInfosTask { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
