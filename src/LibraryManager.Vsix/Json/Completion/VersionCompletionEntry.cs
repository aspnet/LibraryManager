// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.Web.Editor.Completion;
using Microsoft.VisualStudio.Text;
using Microsoft.Web.LibraryManager.Providers.Unpkg;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal class VersionCompletionEntry : SimpleCompletionEntry
    {
        internal SemanticVersion SemVersion { get; private set; }

        public VersionCompletionEntry(string displayText, string insertionText, string description, ImageMoniker moniker, ITrackingSpan span, IIntellisenseSession session, int specificVersion)
            : base(displayText, insertionText, description, moniker, span, session, specificVersion)
        {
            if (!string.IsNullOrEmpty(displayText))
            {
                SemVersion = SemanticVersion.Parse(displayText);
            }
        }

        protected override int InternalCompareTo(CompletionEntry other)
        {
            VersionCompletionEntry otherEntry = other as VersionCompletionEntry;

            if (otherEntry == null)
            {
                return 1;
            }

            if (SemVersion == null)
            {
                if (otherEntry.SemVersion != null)
                {
                    return -1;
                }
                else
                {
                    return base.InternalCompareTo(other);
                }
            }
            else
            {
                if (otherEntry.SemVersion != null)
                {
                    return -SemVersion.CompareTo(otherEntry.SemVersion);
                }
                else
                {
                    return 1;
                }
            }
        }
    }
}
