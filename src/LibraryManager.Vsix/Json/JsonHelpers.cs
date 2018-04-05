// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.JSON.Core.Parser.TreeItems;
using System.Linq;
using System.Collections.Generic;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal static class JsonHelpers
    {
        public static bool TryGetInstallationState(JSONObject parent, out ILibraryInstallationState installationState)
        {
            installationState = null;

            if (parent == null)
            {
                return false;
            }

            var state = new LibraryInstallationState();

            foreach (JSONMember child in parent.Children.OfType<JSONMember>())
            {
                switch (child.UnquotedNameText)
                {
                    case ManifestConstants.Provider:
                        state.ProviderId = child.UnquotedValueText;
                        break;
                    case ManifestConstants.Library:
                        state.LibraryId = child.UnquotedValueText;
                        break;
                    case ManifestConstants.Destination:
                        state.DestinationPath = child.UnquotedValueText;
                        break;
                    case ManifestConstants.Files:
                        state.Files = (child.Value as JSONArray)?.Elements.Select(e => e.UnquotedValueText).ToList();
                        break;
                }
            }

            // Check for defaultProvider
            if (string.IsNullOrEmpty(state.ProviderId))
            {
                IEnumerable<JSONMember> rootMembers = parent.Parent?.FindType<JSONObject>()?.Children?.OfType<JSONMember>();

                if (rootMembers != null)
                {
                    foreach (JSONMember child in rootMembers)
                    {
                        if (child.UnquotedNameText == "defaultProvider")
                            state.ProviderId = child.UnquotedValueText;
                    }
                }
            }

            // Check for defaultDestination
            if (string.IsNullOrEmpty(state.DestinationPath))
            {
                IEnumerable<JSONMember> rootMembers = parent.Parent?.FindType<JSONObject>()?.Children?.OfType<JSONMember>();

                if (rootMembers != null)
                {
                    foreach (JSONMember child in rootMembers)
                    {
                        if (child.UnquotedNameText == ManifestConstants.DefaultDestination)
                            state.DestinationPath = child.UnquotedValueText;
                    }
                }
            }

            installationState = state;

            return !string.IsNullOrEmpty(installationState.ProviderId);
        }
    }
}
