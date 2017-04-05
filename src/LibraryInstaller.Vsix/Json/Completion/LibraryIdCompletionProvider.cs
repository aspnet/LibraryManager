// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using LibraryInstaller.Contracts;
using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.JSON.Editor.Completion;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace LibraryInstaller.Vsix
{
    [Export(typeof(IJSONCompletionListProvider))]
    [Name(nameof(LibraryIdCompletionProvider))]
    class LibraryIdCompletionProvider : BaseCompletionProvider
    {
        private static ImageMoniker _libraryIcon = KnownMonikers.Package;

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

            if (!JsonHelpers.TryGetInstallationState(parent, out ILibraryInstallationState state))
                yield break;

            var dependencies = Dependencies.FromConfigFile(ConfigFilePath);
            IProvider provider = dependencies.GetProvider(state.ProviderId);
            ILibraryCatalog catalog = provider?.GetCatalog();

            if (catalog == null)
                yield break;

            int caretPosition = context.Session.TextView.Caret.Position.BufferPosition - member.Value.Start - 1;

            Task<CompletionSet> task = catalog.GetLibraryCompletionSetAsync(member.UnquotedValueText, caretPosition);
            int count = 0;

            if (task.IsCompleted)
            {
                CompletionSet span = task.Result;

                if (span.Completions != null)
                {
                    foreach (CompletionItem item in span.Completions)
                    {
                        yield return new SimpleCompletionEntry(item.DisplayText, item.InsertionText, item.Description, _libraryIcon, context.Session, ++count);
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
                        CompletionSet span = task.Result;

                        if (span.Completions != null)
                        {
                            var results = new List<JSONCompletionEntry>();

                            foreach (CompletionItem item in span.Completions)
                            {
                                results.Add(new SimpleCompletionEntry(item.DisplayText, item.InsertionText, item.Description, _libraryIcon, context.Session, ++count));
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