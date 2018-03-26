// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.JSON.Editor.Completion;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.LibraryManager.Contracts;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

namespace Microsoft.Web.LibraryManager.Vsix
{
    [Export(typeof(IJSONCompletionListProvider))]
    [Name(nameof(LibraryIdCompletionProvider))]
    internal class LibraryIdCompletionProvider : BaseCompletionProvider
    {
        private static readonly ImageMoniker _libraryIcon = KnownMonikers.Method;
        private static readonly ImageMoniker _folderIcon = KnownMonikers.FolderClosed;

        public override JSONCompletionContextType ContextType
        {
            get { return JSONCompletionContextType.PropertyValue; }
        }

        protected override IEnumerable<JSONCompletionEntry> GetEntries(JSONCompletionContext context)
        {
            var member = context.ContextItem as JSONMember;

            if (member == null || member.UnquotedNameText != ManifestConstants.Library)
            {
                yield break;
            }

            var parent = member.Parent as JSONObject;

            if (!JsonHelpers.TryGetInstallationState(parent, out ILibraryInstallationState state))
            {
                yield break;
            }

            var dependencies = Dependencies.FromConfigFile(ConfigFilePath);
            IProvider provider = dependencies.GetProvider(state.ProviderId);
            ILibraryCatalog catalog = provider?.GetCatalog();

            if (catalog == null)
            {
                yield break;
            }

            // member.Value is null when there is no value yet, e.g. when typing a space at "library":|
            // where | represents caret position. In this case, set caretPosition to "1" to short circuit execution of this function
            // and return no entries (member.UnquotedValueText will be empty string in that case).
            int caretPosition = member.Value != null ? context.Session.TextView.Caret.Position.BufferPosition - member.Value.Start - 1 : 1;

            if (caretPosition > member.UnquotedValueText.Length)
            {
                yield break;
            }

            Task<CompletionSet> task = catalog.GetLibraryCompletionSetAsync(member.UnquotedValueText, caretPosition);
            int count = 0;

            if (task.IsCompleted)
            {
                CompletionSet set = task.Result;
                int start = member.Value.Start;
                ITrackingSpan trackingSpan = context.Snapshot.CreateTrackingSpan(start + 1 + set.Start, set.Length, SpanTrackingMode.EdgeInclusive);

                if (set.Completions != null)
                {
                    foreach (CompletionItem item in set.Completions)
                    {
                        string insertionText = item.InsertionText.Replace("\\\\", "\\").Replace("\\", "\\\\");
                        ImageMoniker moniker = item.DisplayText.EndsWith("/") || item.DisplayText.EndsWith("\\") ? _folderIcon : _libraryIcon;
                        yield return new SimpleCompletionEntry(item.DisplayText, insertionText, item.Description, moniker, trackingSpan, context.Session, ++count);
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
                        CompletionSet set = task.Result;
                        int start = member.Value.Start;
                        ITrackingSpan trackingSpan = context.Snapshot.CreateTrackingSpan(start + 1 + set.Start, set.Length, SpanTrackingMode.EdgeExclusive);

                        if (set.Completions != null)
                        {
                            var results = new List<JSONCompletionEntry>();

                            foreach (CompletionItem item in set.Completions)
                            {
                                string insertionText = item.InsertionText.Replace("\\", "\\\\");
                                ImageMoniker moniker = item.DisplayText.EndsWith("/") || item.DisplayText.EndsWith("\\") ? _folderIcon : _libraryIcon;
                                results.Add(new SimpleCompletionEntry(item.DisplayText, insertionText, item.Description, moniker, trackingSpan, context.Session, ++count));
                            }

                            UpdateListEntriesSync(context, results);
                        }
                    }
                });
            }
        }
    }
}