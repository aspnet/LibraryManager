using LibraryInstaller.Contracts;
using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.JSON.Editor.Completion;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace LibraryInstaller.Vsix
{
    [Export(typeof(IJSONCompletionListProvider))]
    [Name(nameof(LibraryIdCompletionProvider))]
    class LibraryIdCompletionProvider : BaseCompletionProvider
    {
        private static BitmapSource _libraryIcon = WpfUtil.GetIconForImageMoniker(KnownMonikers.Package, 16, 16);

        public override JSONCompletionContextType ContextType
        {
            get { return JSONCompletionContextType.PropertyValue; }
        }

        protected override IEnumerable<JSONCompletionEntry> GetEntries(JSONCompletionContext context)
        {
            var member = context.ContextItem as JSONMember;

            if (member == null || member.UnquotedNameText != "id")
                yield break;

            var parent = member.Parent as JSONObject;

            if (!TryGetProviderId(parent, out string providerId, out string libraryId))
                yield break;

            var dependencies = Dependencies.FromConfigFile(ConfigFilePath);
            IProvider provider = dependencies.GetProvider(providerId);
            ILibraryCatalog catalog = provider?.GetCatalog();

            if (catalog == null)
                yield break;

            int caretPosition = context.Session.TextView.Caret.Position.BufferPosition - member.Value.Start - 1;

            Task<CompletionSpan> task = catalog.GetCompletionsAsync(member.UnquotedValueText, caretPosition);
            int count = 0;

            if (task.IsCompleted)
            {
                CompletionSpan span = task.Result;

                if (span.Completions != null)
                {
                    foreach (string value in span.Completions.Keys)
                    {
                        yield return new SimpleCompletionEntry(value, span.Completions[value], _libraryIcon, context.Session, ++count);
                    }
                }
            }
            else
            {
                yield return new SimpleCompletionEntry(Resources.Text.Loading, KnownMonikers.Loading, context.Session);

                task.ContinueWith((a) =>
                {
                    if (!context.Session.IsDismissed)
                    {
                        CompletionSpan span = task.Result;

                        if (span.Completions != null)
                        {
                            var results = new List<JSONCompletionEntry>();

                            foreach (string value in span.Completions.Keys)
                            {
                                results.Add(new SimpleCompletionEntry(value, span.Completions[value], _libraryIcon, context.Session, ++count));
                            }

                            UpdateListEntriesSync(context, results);
                        }
                    }
                });
            }

            Telemetry.TrackUserTask("completionlibraryid");
        }
    }
}