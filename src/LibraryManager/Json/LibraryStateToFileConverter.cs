// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Text;
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

            (string name, string version) = LibraryIdToNameAndVersionConverter.Instance.GetLibraryNameAndVersion(stateOnDisk.LibraryId, provider);
            string destination = string.IsNullOrEmpty(stateOnDisk.DestinationPath) ? ExpandDestination(_defaultDestination, name, version) : stateOnDisk.DestinationPath;

            var state = new LibraryInstallationState()
            {
                Name = name,
                Version = version,
                IsUsingDefaultDestination = string.IsNullOrEmpty(stateOnDisk.DestinationPath),
                IsUsingDefaultProvider = string.IsNullOrEmpty(stateOnDisk.ProviderId),
                ProviderId = provider,
                DestinationPath = destination,
                Files = stateOnDisk.Files
            };

            return state;
        }

        /// <summary>
        /// Expands [Name] and [Version] tokens in the DefaultDestination
        /// </summary>
        /// <param name="destination">The default destination string</param>
        /// <param name="name">Package name</param>
        /// <param name="version">Package version</param>
        /// <returns></returns>
        [SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity", Justification = "Not available on net481, not needed here (caseless)")]
        private string ExpandDestination(string destination, string name, string version)
        {
            if (!destination.Contains("["))
            {
                return destination;
            }

            // if the name contains a slash (either filesystem or scoped packages),
            // trim that and only take the last segment.
            int cutIndex = name.LastIndexOfAny(['/', '\\']);

            StringBuilder stringBuilder = new StringBuilder(destination);
            stringBuilder.Replace("[Name]", cutIndex == -1 ? name : name.Substring(cutIndex + 1));
            stringBuilder.Replace("[Version]", version);
            return stringBuilder.ToString();
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
