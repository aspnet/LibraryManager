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
