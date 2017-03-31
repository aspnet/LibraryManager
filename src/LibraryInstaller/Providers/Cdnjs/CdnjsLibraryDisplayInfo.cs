using LibraryInstaller.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryInstaller.Providers.Cdnjs
{
    internal class CdnjsLibraryDisplayInfo : ILibraryDisplayInfo
    {
        private string _libraryId, _providerId;
        private Asset _asset;

        public CdnjsLibraryDisplayInfo(Asset asset, string libraryId, string providerId)
        {
            _asset = asset;
            _libraryId = libraryId;
            _providerId = providerId;
        }

        public string Version => _asset.Version;

        public Task<ILibrary> GetLibraryAsync(CancellationToken cancellationToken)
        {
            var library = new CdnjsLibrary
            {
                Version = Version,
                Files = _asset.Files.ToDictionary(k => k, b => b == _asset.DefaultFile),
                Id = _libraryId,
                ProviderId = _providerId
            };

            return Task.FromResult<ILibrary>(library);
        }
    }
}
