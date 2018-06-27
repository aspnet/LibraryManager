// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.Web.Editor.Completion;
using Microsoft.VisualStudio.Text;
using System.Diagnostics;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal class LibraryIdCompletionEntry : SimpleCompletionEntry
    {
        internal int Major { get; private set; }

        internal int Minor { get; private set; }

        internal int Patch { get; private set; }

        internal bool IsValid { get; private set; }

        public LibraryIdCompletionEntry(string displayText, string insertionText, ImageMoniker moniker, IIntellisenseSession session)
            : base(displayText, insertionText, moniker, session)
        {
        }

        public LibraryIdCompletionEntry(string displayText, string insertionText, string description, ImageMoniker moniker, ITrackingSpan span, IIntellisenseSession session, bool versionSpecific = false)
            : base(displayText, insertionText, description, moniker, span, session)
        {
            if (versionSpecific)
            {
                SetVersion(insertionText);
            }
        }

        protected override int InternalCompareTo(CompletionEntry other)
        {
            if (other == null)
            {
                return 1;
            }

            LibraryIdCompletionEntry otherEntry = other as LibraryIdCompletionEntry;

            if (IsValid)
            {
                int result = -Major.CompareTo(otherEntry.Major);

                if (result != 0)
                {
                    return result;
                }

                result = -Minor.CompareTo(otherEntry.Minor);

                if (result != 0)
                {
                    return result;
                }

                result = -Patch.CompareTo(otherEntry.Patch);

                if (result != 0)
                {
                    return result;
                }
            }

            return base.InternalCompareTo(other);
        }

        private void SetVersion(string insertionText)
        {
            if (!string.IsNullOrEmpty(insertionText))
            {
                int atIndex = insertionText.IndexOf('@');

                Debug.Assert(atIndex >= 0);

                string versionText = insertionText.Substring(atIndex + 1);
                string[] versionParts = versionText.Split('.');

                IsValid = true;

                if (versionParts.Length > 0)
                {
                    int major;
                    int.TryParse(versionParts[0], out major);
                    Major = major;
                }

                if (versionParts.Length > 1)
                {
                    int minor;
                    int.TryParse(versionParts[1], out minor);
                    Minor = minor;
                }

                if (versionParts.Length > 2)
                {
                    int patch;
                    int.TryParse(versionParts[2], out patch);
                    Patch = patch;
                }
            }
        }
    }
}
