// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Microsoft.Web.LibraryManager.Contracts
{
    /// <summary>
    /// A span for use by completion
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CompletionSet
    {
        // IMPORTANT: Do not change the order of the fields below!!!

        /// <summary>
        /// The start position of the span
        /// </summary>
        public int Start;

        /// <summary>
        /// The length of the span
        /// </summary>
        public int Length;

        /// <summary>
        /// The list of completions for the span
        /// </summary>
        public IEnumerable<CompletionItem> Completions;

        /// <summary>
        /// The type of the completion item sorting.
        /// </summary>
        public CompletionSortOrder CompletionType;
    }

    /// <summary>
    /// The completion item to show in supporting editors.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CompletionItem
    {
        // IMPORTANT: Do not change the order of the fields below!!!

        /// <summary>
        /// The display text of the completion item.
        /// </summary>
        public string DisplayText;

        /// <summary>
        /// The insertion text is what is being insert when completion is committed.
        /// </summary>
        public string InsertionText;

        /// <summary>
        /// The description is shown in tooltips and parameter info.
        /// </summary>
        public string Description;
    }

    /// <summary>
    /// The completion sort order is to indicate the sorting type of the completion item.
    /// </summary>
    public enum CompletionSortOrder
    {
        /// <summary>The completion item is alphabetical sorted.</summary>
        Alphabetical,
        /// <summary>The completion item is for library version completion.</summary>
        Version,
        /// <summary>The completion item is sorted by providers.</summary>
        AsSpecified
    }
}
