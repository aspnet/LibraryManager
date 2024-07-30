// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.WebTools.Languages.Shared.Editor.Completion;

namespace Microsoft.Web.LibraryManager.Vsix.Json.Completion
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

            // The version completion list should be displayed in descending order.
            int result = -CompareSemanticVersion(SemVersion, otherEntry?.SemVersion);

            return (result == 0) ? base.InternalCompareTo(other) : result;
        }

        private int CompareSemanticVersion(SemanticVersion selfSemVersion, SemanticVersion otherSemVersion)
        {      
            if (selfSemVersion == null)
            {
                if (otherSemVersion != null)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                if (otherSemVersion != null)
                {
                    return selfSemVersion.CompareTo(otherSemVersion);
                }
                else
                {
                    return 1;
                }
            }
        }
    }
}
