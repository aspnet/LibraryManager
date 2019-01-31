// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
