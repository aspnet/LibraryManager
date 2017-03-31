using Microsoft.JSON.Editor.Completion;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using System.Windows.Media;
using Microsoft.Web.Editor.Completion;

namespace LibraryInstaller.Vsix
{
    class SimpleCompletionEntry : JSONCompletionEntry
    {
        private int _specificVersion;

        public SimpleCompletionEntry(string text, ImageMoniker moniker, IIntellisenseSession session)
            : this(text, null, WpfUtil.GetIconForImageMoniker(moniker, 16, 16), session)
        { }

        public SimpleCompletionEntry(string text, ImageSource glyph, IIntellisenseSession session)
            : this(text, text, glyph, session)
        { }

        public SimpleCompletionEntry(string text, string insertionText, ImageSource glyph, IIntellisenseSession session, int specificVersion = 0)
            : base(text, "\"" + insertionText + "\"", null, glyph, null, false, session as ICompletionSession)
        {
            _specificVersion = specificVersion;
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