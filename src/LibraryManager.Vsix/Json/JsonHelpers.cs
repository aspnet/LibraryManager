// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Json;
using Microsoft.WebTools.Languages.Json.Parser.Nodes;
using Microsoft.WebTools.Languages.Shared.Parser.Nodes;
using Microsoft.WebTools.Languages.Shared.Utility;

namespace Microsoft.Web.LibraryManager.Vsix.Json
{
    internal static class JsonHelpers
    {
        public static Node GetNodeBeforePosition(int pos, ComplexNode parent)
        {
            Node node = null;
            SortedNodeList<Node> children = GetChildren(parent);
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
                    node = children[i];

                    if (node is ComplexNode complexNode)
                    {
                        // Recurse to find the deepest node
                        node = GetNodeBeforePosition(pos, complexNode);
                    }
                }
            }

            return node;
        }

        public static int FindInsertIndex(SortedNodeList<Node> nodes, int rangeStart)
        {
            int min = 0;
            int max = nodes.Count - 1;

            while (min <= max)
            {
                int mid = (min + max) / 2;
                int start = nodes[mid].Start;

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

        public static SortedNodeList<Node> GetChildren(Node node)
        {
            SortedNodeList<Node> children = new SortedNodeList<Node>();

            if (node != null)
            {
                for (int i = 0; i < node.SlotCount; i++)
                {
                    children.Add(node.GetNodeSlot(i));
                }
            }

            return children;
        }

        public static bool TryGetInstallationState(ObjectNode parent, out ILibraryInstallationState installationState, string defaultProvider = null)
        {
            installationState = null;

            if (parent == null)
            {
                return false;
            }

            var state = new LibraryInstallationStateOnDisk();

            SortedNodeList<Node> children = GetChildren(parent);

            string GetCanonicalizedValue(BlockChildNode m)
            {
                if (m.Value is TokenNode value)
                {
                    return value.GetCanonicalizedText();
                }
                return m.UnquotedValueText;
            }

            foreach (MemberNode child in children.OfType<MemberNode>())
            {
                // note: the Json parser escapes backslashes in the node's Value text,
                // so we need to unescape those so that they match the value from the manifest.
                switch (child.UnquotedNameText)
                {
                    case ManifestConstants.Provider:
                        state.ProviderId = GetCanonicalizedValue(child);
                        break;
                    case ManifestConstants.Library:
                        state.LibraryId = GetCanonicalizedValue(child);
                        break;
                    case ManifestConstants.Destination:
                        state.DestinationPath = GetCanonicalizedValue(child);
                        break;
                    case ManifestConstants.Files:
                        state.Files = (child.Value as ArrayNode)?.Elements.Select(e => GetCanonicalizedValue(e)).ToList();
                        break;
                }
            }

            children = GetChildren(parent.Parent?.FindType<ObjectNode>());
            IEnumerable<MemberNode> rootMembers = children?.OfType<MemberNode>();

            // Check for defaultProvider
            if (string.IsNullOrEmpty(state.ProviderId))
            {
                if (rootMembers != null)
                {
                    foreach (MemberNode child in rootMembers)
                    {
                        if (child.UnquotedNameText == "defaultProvider")
                            state.ProviderId = child.UnquotedValueText;
                    }
                }
            }

            // Check for defaultDestination
            if (string.IsNullOrEmpty(state.DestinationPath))
            {
                if (rootMembers != null)
                {
                    foreach (MemberNode child in rootMembers)
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
