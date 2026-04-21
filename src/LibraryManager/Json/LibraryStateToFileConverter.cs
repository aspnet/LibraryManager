// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.LibraryNaming;
using Microsoft.Web.LibraryManager.Utilities;

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

            (string name, string version) = LibraryIdToNameAndVersionConverter.Instance.GetLibraryNameAndVersion(stateOnDisk.LibraryId, provider);
            string destination = string.IsNullOrEmpty(stateOnDisk.DestinationPath) ? PathTemplateUtility.ExpandPathTemplate(_defaultDestination, name, version) : stateOnDisk.DestinationPath;

            var state = new LibraryInstallationState()
            {
                Name = name,
                Version = version,
                IsUsingDefaultDestination = string.IsNullOrEmpty(stateOnDisk.DestinationPath),
                IsUsingDefaultProvider = string.IsNullOrEmpty(stateOnDisk.ProviderId),
                ProviderId = provider,
                DestinationPath = destination,
                Files = stateOnDisk.Files,
                FileMappings = stateOnDisk.FileMappings?.Select(f => new Contracts.FileMapping { Destination = f.Destination, Root = f.Root, Files = f.Files }).ToList(),
            };

            return state;
        }

        public LibraryInstallationStateOnDisk ConvertToLibraryInstallationStateOnDisk(ILibraryInstallationState state)
        {
            if (state == null)
            {
                return null;
            }

            string provider = string.IsNullOrEmpty(state.ProviderId) ? _defaultProvider : state.ProviderId;
            var serializeState = new LibraryInstallationStateOnDisk()
            {
                ProviderId = state.IsUsingDefaultProvider ? null : state.ProviderId,
                DestinationPath = state.IsUsingDefaultDestination ? null : state.DestinationPath,
                Files = state.Files,
                LibraryId = LibraryIdToNameAndVersionConverter.Instance.GetLibraryId(state.Name, state.Version, provider),
                FileMappings = state.FileMappings?.Select(f => new FileMapping { Destination = f.Destination, Root = f.Root, Files = f.Files }).ToList(),
            };

            if (serializeState is { FileMappings: { Count: 0} })
            {
                // if FileMappings is empty, omit it from serialization
                serializeState.FileMappings = null;
            }

            return serializeState;
        }
    }
}
