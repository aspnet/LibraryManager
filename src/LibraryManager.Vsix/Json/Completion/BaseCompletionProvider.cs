﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.WebTools.Languages.Json.Editor.Completion;
using Microsoft.WebTools.Languages.Shared.Editor.Completion;
using Microsoft.WebTools.Languages.Shared.Editor.Host;

namespace Microsoft.Web.LibraryManager.Vsix.Json.Completion
{
    internal abstract class BaseCompletionProvider : IJsonCompletionListProvider
    {
        private static readonly IEnumerable<JsonCompletionEntry> Empty = Enumerable.Empty<JsonCompletionEntry>();

        [Import]
        public ITextDocumentFactoryService DocumentService { get; set; }

        public abstract JsonCompletionContextType ContextType { get; }

        public string ConfigFilePath { get; private set; }

        public IEnumerable<JsonCompletionEntry> GetListEntries(JsonCompletionContext context)
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

            return Empty;
        }

        protected abstract IEnumerable<JsonCompletionEntry> GetEntries(JsonCompletionContext context);

        protected void UpdateListEntriesSync(JsonCompletionContext context, IEnumerable<JsonCompletionEntry> allEntries)
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
