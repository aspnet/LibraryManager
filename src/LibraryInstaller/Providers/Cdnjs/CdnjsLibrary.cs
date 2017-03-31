using LibraryInstaller.Contracts;
using System.Collections.Generic;

namespace LibraryInstaller.Providers.Cdnjs
{
    internal class CdnjsLibrary : ILibrary
    {
        public string Id { get; set; }
        public string ProviderId { get; set; }
        public string Version { get; set; }
        public IReadOnlyDictionary<string, bool> Files { get; set; }

        public override string ToString()
        {
            return Id;
        }
    }
}
