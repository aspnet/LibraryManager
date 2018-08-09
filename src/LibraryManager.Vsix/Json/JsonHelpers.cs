// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.JSON.Core.Parser;
using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Json;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal static class JsonHelpers
    {
        internal static JSONParseItem GetItemBeforePosition(int pos, JSONComplexItem parentItem)
        {
            JSONParseItem item = null;
            JSONParseItemList children = parentItem.Children;
            int start = 0;

            if (children.Any())
            {
                start = children[0].Start;
            }

            if (start < pos)
            {
                int i = FindInsertIndex(children, pos) - 1;

                if (i >= 0)
                {
                    item = children[i];

                    if (item is JSONComplexItem complexItem)
                    {
                        // Recurse to find the deepest item
                        item = GetItemBeforePosition(pos, complexItem);
                    }
                }
            }

            return item;
        }

        internal static int FindInsertIndex(JSONParseItemList jsonParseItems, int rangeStart)
        {
            int min = 0;
            int max = jsonParseItems.Count - 1;

            while (min <= max)
            {
                int mid = (min + max) / 2;
                int start = jsonParseItems[mid].Start;

                if (rangeStart <= start)
                {
                    max = mid - 1;
                }
                else
                {
                    min = mid + 1;
                }
            }

            return max + 1;
        }

        public static bool TryGetInstallationState(JSONObject parent, out ILibraryInstallationState installationState, string defaultProvider = null)
        {
            installationState = null;

            if (parent == null)
            {
                return false;
            }

            var state = new LibraryInstallationStateOnDisk();

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

            var converter = new LibraryStateToFileConverter(defaultProvider, defaultDestination: null);
            installationState = converter.ConvertToLibraryInstallationState(state);

            return !string.IsNullOrEmpty(installationState.ProviderId);
        }
    }
}
