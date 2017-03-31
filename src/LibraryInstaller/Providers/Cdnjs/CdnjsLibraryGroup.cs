// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using LibraryInstaller.Contracts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryInstaller.Providers.Cdnjs
{
    internal class CdnjsLibraryGroup : ILibraryGroup
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }

        public Task<IReadOnlyList<ILibraryDisplayInfo>> GetDisplayInfosAsync(CancellationToken cancellationToken)
        {
            return DisplayInfosTask?.Invoke(cancellationToken) ?? Task.FromResult<IReadOnlyList<ILibraryDisplayInfo>>(new ILibraryDisplayInfo[0]);
        }

        public Func<CancellationToken, Task<IReadOnlyList<ILibraryDisplayInfo>>> DisplayInfosTask { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
