// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryInstaller.Contracts;
using Microsoft.JSON.Core.Parser;
using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.VisualStudio.JSON.Package.SuggestedActions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Microsoft.Web.LibraryInstaller.Vsix
{
    [Export(typeof(IJSONSuggestedActionProvider))]
    [Name(nameof(SuggestedActionProvider))]
    internal class SuggestedActionProvider : IJSONSuggestedActionProvider
    {
        public ILibraryInstallationState InstallationState;
        public JSONObject LibraryObject;
        public string ConfigFilePath;
        public ITextView TextView;
        public ITextBuffer TextBuffer;

        [Import]
        public ITextDocumentFactoryService DocumentService { get; set; }

        public IEnumerable<ISuggestedAction> GetSuggestedActions(ITextView textView, ITextBuffer textBuffer, int caretPosition, JSONParseItem parseItem)
        {
            TextView = textView;
            TextBuffer = textBuffer;

            yield return new UninstallSuggestedAction(this);

            var update = new UpdateSuggestedActionSet(this);

            if (update.HasActionSets)
                yield return update;
        }

        public bool HasSuggestedActions(ITextView textView, ITextBuffer textBuffer, int caretPosition, JSONParseItem parseItem)
        {
            if (!DocumentService.TryGetTextDocument(textView.TextBuffer, out var doc))
                return false;

            JSONObject parent = parseItem.FindType<JSONObject>();

            if (!(parent?.Parent is JSONArrayElement) || !parent.IsValid)
            {
                return false;
            }

            if (!JsonHelpers.TryGetInstallationState(parent, out InstallationState))
            {
                return false;
            }

            ConfigFilePath = doc.FilePath;
            LibraryObject = parent;

            return !string.IsNullOrEmpty(InstallationState.LibraryId);
        }
    }
}
