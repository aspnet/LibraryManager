// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
