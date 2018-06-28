// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.Web.Editor.Completion;
using Microsoft.VisualStudio.Text;
using System.Diagnostics;
using Microsoft.Web.LibraryManager.Providers.Unpkg;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal class VersionCompletionEntry : SimpleCompletionEntry
    {
        internal string VersionText { get; private set; }

        public VersionCompletionEntry(string displayText, string insertionText, string description, ImageMoniker moniker, ITrackingSpan span, IIntellisenseSession session, int specificVersion)
            : base(displayText, insertionText, description, moniker, span, session, specificVersion)
        {
            VersionText = displayText;
        }

        protected override int InternalCompareTo(CompletionEntry other)
        {
            VersionCompletionEntry otherEntry = other as VersionCompletionEntry;

            if (otherEntry == null)
            {
                return 1;
            }

            if (!string.IsNullOrEmpty(VersionText))
            {
                string otherVersionText = otherEntry.VersionText;
                SemanticVersion selfSemanticVersion = SemanticVersion.Parse(VersionText);
                SemanticVersion otherSemanticVersion = SemanticVersion.Parse(otherVersionText);

                return -selfSemanticVersion.CompareTo(otherSemanticVersion);
            }

            return base.InternalCompareTo(other);
        }
    }
}
