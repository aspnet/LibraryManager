// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.JSON.Editor.Completion;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal class SimpleCompletionEntry : JSONCompletionEntry
    {
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

        public SimpleCompletionEntry(string displayText, string insertionText, string description, ImageMoniker moniker, IIntellisenseSession session)
            : base(displayText, "\"" + insertionText + "\"", description, null, null, false, session as ICompletionSession)
        {
            SetIconMoniker(moniker);
        }

        public SimpleCompletionEntry(string displayText, string insertionText, string description, ImageMoniker moniker, ITrackingSpan span, IIntellisenseSession session)
            : base(displayText, "\"" + insertionText + "\"", description, null, null, false, session as ICompletionSession)
        {
            SetIconMoniker(moniker);
            ApplicableTo = span;
        }

        public SimpleCompletionEntry(string displayText, string insertionText, ImageMoniker moniker, ITrackingSpan span, IIntellisenseSession session)
         : base(displayText, "\"" + insertionText + "\"", null, null, null, false, session as ICompletionSession)
        {
            SetIconMoniker(moniker);
            ApplicableTo = span;
        }

        public override bool IsCommitChar(char typedCharacter)
        {
            return typedCharacter == '/' || typedCharacter == '\\';
        }
    }
}
