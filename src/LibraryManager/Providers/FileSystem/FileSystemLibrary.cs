// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Web.LibraryManager.Contracts;
using System.Collections.Generic;

namespace Microsoft.Web.LibraryManager.Providers.FileSystem
{
    internal class FileSystemLibrary : ILibrary
    {
        public string Name { get; set; }
        public string ProviderId { get; set; }
        public string Version => "1.0";
        public IReadOnlyDictionary<string, bool> Files { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
