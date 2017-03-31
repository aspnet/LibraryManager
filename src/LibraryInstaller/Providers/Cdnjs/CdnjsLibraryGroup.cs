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
