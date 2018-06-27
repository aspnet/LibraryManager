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
    internal class LibraryIdCompletionEntry : SimpleCompletionEntry
    {
        internal string NameVersionText { get; private set; }

        public LibraryIdCompletionEntry(string displayText, string insertionText, ImageMoniker moniker, IIntellisenseSession session)
            : base(displayText, insertionText, moniker, session)
        {
            NameVersionText = displayText;
        }

        public LibraryIdCompletionEntry(string displayText, string insertionText, string description, ImageMoniker moniker, ITrackingSpan span, IIntellisenseSession session)
            : base(displayText, insertionText, description, moniker, span, session)
        {
            NameVersionText = displayText;
        }

        protected override int InternalCompareTo(CompletionEntry other)
        {
            LibraryIdCompletionEntry otherEntry = other as LibraryIdCompletionEntry;

            if (otherEntry == null)
            {
                return 1;
            }

            if (!string.IsNullOrEmpty(NameVersionText))
            {
                int atIndex = NameVersionText.IndexOf('@');

                if (atIndex >= 0)
                {
                    Debug.Assert(otherEntry.NameVersionText.IndexOf('@') == atIndex);

                    string selfVersionText = NameVersionText.Substring(atIndex + 1);
                    string otherVersionText = otherEntry.NameVersionText.Substring(atIndex + 1);
                    SemanticVersion selfSemanticVersion = SemanticVersion.Parse(selfVersionText);
                    SemanticVersion otherSemanticVersion = SemanticVersion.Parse(otherVersionText);

                    return -selfSemanticVersion.CompareTo(otherSemanticVersion);
                }
            }

            return base.InternalCompareTo(other);
        }
    }
}
