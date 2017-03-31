using LibraryInstaller.Contracts;
using System.Collections.Generic;

namespace LibraryInstaller.Providers.FileSystem
{
    internal class FileSystemLibrary : ILibrary
    {
        public string Id { get; set; }
        public string ProviderId { get; set; }
        public string Version => "1.0";
        public IReadOnlyDictionary<string, bool> Files { get; set; }

        public override string ToString()
        {
            return Id;
        }
    }
}
