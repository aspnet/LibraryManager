// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Windows.Media;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.WebTools.Languages.Json.Editor.Completion;
using Microsoft.WebTools.Languages.Shared.Editor.Completion;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal class SimpleCompletionEntry : JsonCompletionEntry
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

        public SimpleCompletionEntry(string displayText, string insertionText, ImageMoniker moniker, IIntellisenseSession session)
            : base(displayText, insertionText, null, null, null, false, session as ICompletionSession)
        {
        }

        public SimpleCompletionEntry(string displayText, string insertionText, string description, ImageMoniker moniker, IIntellisenseSession session, int specificVersion = 0)
            : base(displayText, "\"" + insertionText + "\"", description, null, null, false, session as ICompletionSession)
        {
            SetIconMoniker(moniker);
            _specificVersion = specificVersion;
        }

        public SimpleCompletionEntry(string displayText, string insertionText, string description, ImageMoniker moniker, ITrackingSpan span, IIntellisenseSession session, int specificVersion = 0)
            : base(displayText, "\"" + insertionText + "\"", description, null, null, false, session as ICompletionSession)
        {
            SetIconMoniker(moniker);
            ApplicableTo = span;
            _specificVersion = specificVersion;
        }

        public SimpleCompletionEntry(string displayText, string insertionText, ImageMoniker moniker, ITrackingSpan span, IIntellisenseSession session, int specificVersion = 0)
         : base(displayText, "\"" + insertionText + "\"", null, null, null, false, session as ICompletionSession)
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
