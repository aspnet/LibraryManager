// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.JSON.Editor.Completion;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.Web.Editor.Completion;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using System.Diagnostics;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal class SimpleCompletionEntry : JSONCompletionEntry
    {
        internal VersionItem version;

        public SimpleCompletionEntry(string text, ImageSource glyph, IIntellisenseSession session)
            : base(text, "\"" + text + "\"", null, glyph, null, false, session as ICompletionSession)
        {
        }

        public SimpleCompletionEntry(string text, ImageMoniker moniker, IIntellisenseSession session)
            : this(text, text, null, moniker, session)
        {
        }

        public SimpleCompletionEntry(string displayText, string insertionText, ImageMoniker moniker, IIntellisenseSession session)
            : base(displayText, insertionText, null, null, null, false, session as ICompletionSession)
        {
        }

        public SimpleCompletionEntry(string displayText, string insertionText, string description, ImageMoniker moniker, IIntellisenseSession session, bool versionSpecific = false)
            : base(displayText, "\"" + insertionText + "\"", description, null, null, false, session as ICompletionSession)
        {
            SetIconMoniker(moniker);

            if (versionSpecific)
            {
                SetVersion(insertionText);
            }
        }

        public SimpleCompletionEntry(string displayText, string insertionText, string description, ImageMoniker moniker, ITrackingSpan span, IIntellisenseSession session, bool versionSpecific = false)
           : base(displayText, "\"" + insertionText + "\"", description, null, null, false, session as ICompletionSession)
        {
            SetIconMoniker(moniker);
            ApplicableTo = span;

            if (versionSpecific)
            {
                SetVersion(insertionText);
            }
        }

        public SimpleCompletionEntry(string displayText, string insertionText, ImageMoniker moniker, ITrackingSpan span, IIntellisenseSession session, bool versionSpecific = false)
         : base(displayText, "\"" + insertionText + "\"", null, null, null, false, session as ICompletionSession)
        {
            SetIconMoniker(moniker);
            ApplicableTo = span;

            if (versionSpecific)
            {
                SetVersion(insertionText);
            }
        }

        public override bool IsCommitChar(char typedCharacter)
        {
            return typedCharacter == '/' || typedCharacter == '\\';
        }

        protected override int InternalCompareTo(CompletionEntry other)
        {
            var otherEntry = other as SimpleCompletionEntry;

            if (otherEntry == null)
            {
                return 1;
            }

            if (version.IsValid)
            {
                int result = -version.Major.CompareTo(otherEntry.version.Major);

                if (result != 0)
                {
                    return result;
                }

                result = -version.Minor.CompareTo(otherEntry.version.Minor);

                if (result != 0)
                {
                    return result;
                }

                result = -version.Patch.CompareTo(otherEntry.version.Patch);

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

                version.IsValid = true;

                if (versionParts.Length > 0)
                {
                    int major;
                    int.TryParse(versionParts[0], out major);
                    version.Major = major;
                }

                if (versionParts.Length > 1)
                {
                    int minor;
                    int.TryParse(versionParts[1], out minor);
                    version.Minor = minor;
                }

                if (versionParts.Length > 2)
                {
                    int patch;
                    int.TryParse(versionParts[2], out patch);
                    version.Patch = patch;
                }
            }
        }

        internal struct VersionItem
        {
            internal int Major;

            internal int Minor;

            internal int Patch;

            internal bool IsValid;
        }
    }
}
