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

            if (!context.Session.Properties.ContainsProperty(CompletionController.RetriggerCompletion))
            {
                context.Session.Properties.AddProperty(CompletionController.RetriggerCompletion, true);
            }

            if (task.IsCompleted)
            {
                CompletionSet completionSet = task.Result;

                if (completionSet.Completions != null)
                {
                    List<JSONCompletionEntry> results = GetCompletionList(member, context, completionSet, count);

                    foreach (JSONCompletionEntry completionEntry in results)
                    {
                        yield return (completionSet.CompletionType == CompletionSortOrder.Version) ? completionEntry as VersionCompletionEntry : completionEntry as SimpleCompletionEntry;
                    }
                }
            }
            else
            {
                yield return new SimpleCompletionEntry(Resources.Text.Loading, string.Empty, KnownMonikers.Loading, context.Session);

                task.ContinueWith((a) =>
                {
                    if (!context.Session.IsDismissed)
                    {
                        CompletionSet completionSet = task.Result;

                        if (completionSet.Completions != null)
                        {
                            List<JSONCompletionEntry> results = GetCompletionList(member, context, completionSet, count);

                            UpdateListEntriesSync(context, results);
                        }
                    }
                });
            }
        }

        private List<JSONCompletionEntry> GetCompletionList(JSONMember member, JSONCompletionContext context, CompletionSet completionSet, int count)
        {
            int start = member.Value.Start;
            ITrackingSpan trackingSpan = context.Snapshot.CreateTrackingSpan(start + 1 + completionSet.Start, completionSet.Length, SpanTrackingMode.EdgeExclusive);
            bool isVersionCompletion = (completionSet.CompletionType == CompletionSortOrder.Version);

            List<JSONCompletionEntry> results = new List<JSONCompletionEntry>();

            foreach (CompletionItem item in completionSet.Completions)
            {
                string insertionText = item.InsertionText.Replace("\\\\", "\\").Replace("\\", "\\\\");
                ImageMoniker moniker = item.DisplayText.EndsWith("/") || item.DisplayText.EndsWith("\\") ? _folderIcon : _libraryIcon;

                if (isVersionCompletion)
                {
                    results.Add(new VersionCompletionEntry(item.DisplayText, insertionText, item.Description, moniker, trackingSpan, context.Session, ++count));
                }
                else
                {
                    results.Add(new SimpleCompletionEntry(item.DisplayText, insertionText, item.Description, moniker, trackingSpan, context.Session, ++count));
                }
            }

            return results;
        }
    }
}
