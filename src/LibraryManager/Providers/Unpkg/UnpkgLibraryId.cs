namespace Microsoft.Web.LibraryManager.Providers.Unpkg
{
    internal class UnpkgLibraryId
    {
        internal UnpkgLibraryId(string libraryIdText)
        {
            libraryIdText = libraryIdText ?? string.Empty;

            string[] idParts = libraryIdText.Split('@');
            Name = idParts[0];
            Version = idParts.Length > 1 ? idParts[1] : string.Empty;
        }

        public string Name { get;  }

        public string Version { get; }
    }
}
