// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Vsix.Contracts;
using Microsoft.WebTools.Languages.Json.Editor.Completion;
using Microsoft.WebTools.Languages.Json.Parser.Nodes;

namespace Microsoft.Web.LibraryManager.Vsix.Json.Completion
{
    [Export(typeof(IJsonCompletionListProvider))]
    [Name(nameof(LibraryIdCompletionProvider))]
    internal class LibraryIdCompletionProvider : BaseCompletionProvider
    {
        private static readonly ImageMoniker LibraryIcon = KnownMonikers.Method;
        private static readonly ImageMoniker FolderIcon = KnownMonikers.FolderClosed;

        private readonly IDependenciesFactory _dependenciesFactory;

        [ImportingConstructor]
        internal LibraryIdCompletionProvider(IDependenciesFactory dependenciesFactory)
        {
            _dependenciesFactory = dependenciesFactory;
        }

        public override JsonCompletionContextType ContextType
        {
            get { return JsonCompletionContextType.PropertyValue; }
        }

        [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Checked for task completion before calling .Result")]
        protected override IEnumerable<JsonCompletionEntry> GetEntries(JsonCompletionContext context)
        {
            var member = context.ContextNode as MemberNode;

            if (member == null || member.UnquotedNameText != ManifestConstants.Library)
            {
                yield break;
            }

            var parent = member.Parent as ObjectNode;

            if (!JsonHelpers.TryGetInstallationState(parent, out ILibraryInstallationState state))
            {
                yield break;
            }

            IDependencies dependencies = _dependenciesFactory.FromConfigFile(ConfigFilePath);
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
                    List<JsonCompletionEntry> results = GetCompletionList(member, context, completionSet, count);

                    foreach (JsonCompletionEntry completionEntry in results)
                    {
                        yield return completionEntry;
                    }
                }
            }
            else
            {
                yield return new SimpleCompletionEntry(Resources.Text.Loading, string.Empty, KnownMonikers.Loading, context.Session);

                _ = task.ContinueWith((t) =>
                {
                    if (!t.IsCanceled || !t.IsFaulted)
                    {
                        if (!context.Session.IsDismissed)
                        {
                            CompletionSet completionSet = t.Result;

                            if (completionSet.Completions != null)
                            {
                                List<JsonCompletionEntry> results = GetCompletionList(member, context, completionSet, count);

                                UpdateListEntriesSync(context, results);
                            }
                        }
                    }
                }, TaskScheduler.Default);
            }
        }

        private List<JsonCompletionEntry> GetCompletionList(MemberNode memberNode, JsonCompletionContext context, CompletionSet completionSet, int count)
        {
            int start = memberNode.Value.Start;
            ITrackingSpan trackingSpan = context.Snapshot.CreateTrackingSpan(start + 1 + completionSet.Start, completionSet.Length, SpanTrackingMode.EdgeExclusive);
            bool isVersionCompletion = (completionSet.CompletionType == CompletionSortOrder.Version);

            List<JsonCompletionEntry> results = new List<JsonCompletionEntry>();

            foreach (CompletionItem item in completionSet.Completions)
            {
                string insertionText = item.InsertionText.Replace("\\\\", "\\").Replace("\\", "\\\\");
                ImageMoniker moniker = item.DisplayText.EndsWith("/", StringComparison.Ordinal) || item.DisplayText.EndsWith("\\", StringComparison.Ordinal)
                    ? FolderIcon
                    : LibraryIcon;

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
