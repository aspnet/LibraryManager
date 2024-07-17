// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Providers.jsDelivr
{
    internal class JsDelivrLibrary : ILibrary
    {
        public string Name { get; set; }
        public string ProviderId { get; set; }
        public string Version { get; set; }
        public IReadOnlyDictionary<string, bool> Files { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
