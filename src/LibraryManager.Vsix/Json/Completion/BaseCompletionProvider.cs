// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.JSON.Core.Schema;
using Microsoft.JSON.Editor.Completion;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.Web.Editor.Completion;
using Microsoft.Web.Editor.Host;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal abstract class BaseCompletionProvider : IJSONCompletionListProvider
    {
        private static readonly IEnumerable<JSONCompletionEntry> _empty = Enumerable.Empty<JSONCompletionEntry>();

        [Import]
        public ITextDocumentFactoryService DocumentService { get; set; }

        [Import]
        public IJSONSchemaEvaluationReportCache ReportCache { get; set; }

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

        protected void UpdateListEntriesSync(JSONCompletionContext context, IEnumerable<JSONCompletionEntry> allEntries)
        {
            if (context.Session.IsDismissed)
            {
                return;
            }

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
