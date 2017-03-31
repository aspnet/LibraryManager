using LibraryInstaller.Contracts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryInstaller.Providers.FileSystem
{
    internal class FileSystemLibraryGroup : ILibraryGroup
    {
        private string _providerId;

        public FileSystemLibraryGroup(string libraryId, string providerId)
        {
            Name = libraryId;
            _providerId = providerId;
        }

        public string Name { get; }

        public string Description => "";

        public Task<IReadOnlyList<ILibraryDisplayInfo>> GetDisplayInfosAsync(CancellationToken cancellationToken)
        {
            var infos = new List<ILibraryDisplayInfo>
            {
                new FileSystemDisplayInfo(Name, _providerId)
            };

            return Task.FromResult<IReadOnlyList<ILibraryDisplayInfo>>(infos);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
