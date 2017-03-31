using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.JSON.Core.Schema;
using Microsoft.JSON.Editor.Completion;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.Web.Editor.Completion;
using Microsoft.Web.Editor.Host;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;

namespace LibraryInstaller.Vsix
{
    abstract class BaseCompletionProvider : IJSONCompletionListProvider
    {
        static readonly IEnumerable<JSONCompletionEntry> _empty = Enumerable.Empty<JSONCompletionEntry>();

        [Import]
        public ITextDocumentFactoryService DocumentService { get; set; }

        [Import]
        IJSONSchemaEvaluationReportCache _reportCache { get; set; }

        public abstract JSONCompletionContextType ContextType { get; }

        public string ConfigFilePath { get; private set; }

        public IEnumerable<JSONCompletionEntry> GetListEntries(JSONCompletionContext context)
        {
            if (DocumentService.TryGetTextDocument(context.Snapshot.TextBuffer, out ITextDocument document))
            {
                ConfigFilePath = document.FilePath;
                string fileName = Path.GetFileName(document.FilePath);

                if (fileName.Equals(Constants.ConfigFileName, StringComparison.OrdinalIgnoreCase))
                {
                    return GetEntries(context);
                }
            }

            return _empty;
        }

        protected abstract IEnumerable<JSONCompletionEntry> GetEntries(JSONCompletionContext context);

        protected static bool TryGetProviderId(JSONObject parent, out string providerId, out string libraryId)
        {
            providerId = null;
            libraryId = null;

            if (parent == null)
                return false;

            foreach (JSONMember child in parent.Children.OfType<JSONMember>())
            {
                if (child.UnquotedNameText == "provider")
                    providerId = child.UnquotedValueText;
                else if (child.UnquotedNameText == "id")
                    libraryId = child.UnquotedValueText;
            }

            return !string.IsNullOrEmpty(providerId);
        }

        protected void UpdateListEntriesSync(JSONCompletionContext context, IEnumerable<JSONCompletionEntry> allEntries)
        {
            foreach (CompletionSet curCompletionSet in (context.Session as ICompletionSession)?.CompletionSets)
            {
                if (curCompletionSet is WebCompletionSet webCompletionSet)
                {
                    WebEditor.DispatchOnUIThread(() =>
                    {
                        // only delete our completion entries
                        webCompletionSet.UpdateCompletions(s => s is SimpleCompletionEntry, allEntries);
                    });

                    // The UpdateCompletions call above may modify the collection we're enumerating. That's ok, as we're done anyways.
                    break;
                }
            }
        }
    }
}