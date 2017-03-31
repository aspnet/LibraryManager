// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using LibraryInstaller.Contracts;
using System.Collections.Generic;

namespace LibraryInstaller.Vsix
{
    public class LibraryInstallationState : ILibraryInstallationState
    {
        public string LibraryId { get; set; }
        public string ProviderId { get; set; }
        public IReadOnlyList<string> Files { get; set; }
        public string Path { get; set; }
    }
}
