// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.JSON.Editor.Completion;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.Web.Editor.Completion;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;

namespace Microsoft.Web.LibraryInstaller.Vsix
{
    internal class SimpleCompletionEntry : JSONCompletionEntry
    {
        private readonly int _specificVersion;

        public SimpleCompletionEntry(string text, ImageSource glyph, IIntellisenseSession session)
            : base(text, "\"" + text + "\"", null, glyph, null, false, session as ICompletionSession)
        {
        }

        public SimpleCompletionEntry(string text, ImageMoniker moniker, IIntellisenseSession session)
            : this(text, text, null, moniker, session)
        {
        }

        public SimpleCompletionEntry(string text, string insertionText, string description, ImageMoniker moniker, IIntellisenseSession session, int specificVersion = 0)
            : base(text, "\"" + insertionText + "\"", description, null, null, false, session as ICompletionSession)
        {
            SetIconMoniker(moniker);
            _specificVersion = specificVersion;
        }

        public SimpleCompletionEntry(string text, string insertionText, string description, ImageMoniker moniker, ITrackingSpan span, IIntellisenseSession session, int specificVersion = 0)
           : base(text, "\"" + insertionText + "\"", description, null, null, false, session as ICompletionSession)
        {
            SetIconMoniker(moniker);
            ApplicableTo = span;
            _specificVersion = specificVersion;
        }

        public SimpleCompletionEntry(string text, string insertionText, ImageMoniker moniker, ITrackingSpan span, IIntellisenseSession session, int specificVersion = 0)
         : base(text, "\"" + insertionText + "\"", null, null, null, false, session as ICompletionSession)
        {
            SetIconMoniker(moniker);
            ApplicableTo = span;
            _specificVersion = specificVersion;
        }

        public override bool IsCommitChar(char typedCharacter)
        {
            return typedCharacter == '/' || typedCharacter == '\\';
        }

        protected override int InternalCompareTo(CompletionEntry other)
        {
            var otherEntry = other as SimpleCompletionEntry;

            if (_specificVersion != 0 && otherEntry != null)
            {
                return _specificVersion.CompareTo(otherEntry._specificVersion);
            }

            return base.InternalCompareTo(other);
        }
    }
}