using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.LibraryNaming;

namespace Microsoft.Web.LibraryManager.Json
{
    internal class LibraryStateToFileConverter
    {
        private string _defaultProvider;
        private string _defaultDestination;

        public LibraryStateToFileConverter(string defaultProvider, string defaultDestination)
        {
            _defaultProvider = defaultProvider;
            _defaultDestination = defaultDestination;
        }

        public ILibraryInstallationState ConvertToLibraryInstallationState(LibraryInstallationStateOnDisk stateOnDisk)
        {
            if (stateOnDisk == null)
            {
                return null;
            }

            string provider = string.IsNullOrEmpty(stateOnDisk.ProviderId) ? _defaultProvider : stateOnDisk.ProviderId;
            string destination = string.IsNullOrEmpty(stateOnDisk.DestinationPath) ? _defaultDestination : stateOnDisk.DestinationPath;

            var state = new LibraryInstallationState()
            {
                IsUsingDefaultDestination = string.IsNullOrEmpty(stateOnDisk.DestinationPath),
                IsUsingDefaultProvider = string.IsNullOrEmpty(stateOnDisk.ProviderId),
                ProviderId = provider,
                DestinationPath = destination,
                Files = stateOnDisk.Files
            };

            (state.Name, state.Version) = LibraryIdToNameAndVersionConverter.Instance.GetLibraryNameAndVersion(stateOnDisk.LibraryId, provider);

            return state;
        }

        public LibraryInstallationStateOnDisk ConvertToLibraryInstallationStateOnDisk(ILibraryInstallationState state)
        {
            if (state == null)
            {
                return null;
            }

            string provider = string.IsNullOrEmpty(state.ProviderId) ? _defaultProvider : state.ProviderId;
            return new LibraryInstallationStateOnDisk()
            {
                ProviderId = state.IsUsingDefaultProvider ? null : state.ProviderId,
                DestinationPath = state.IsUsingDefaultDestination ? null : state.DestinationPath,
                Files = state.Files,
                LibraryId = LibraryIdToNameAndVersionConverter.Instance.GetLibraryId(state.Name, state.Version, provider)
            };
        }
    }
}
