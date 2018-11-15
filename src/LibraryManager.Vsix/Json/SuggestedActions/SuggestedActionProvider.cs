// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.WebTools.Languages.Json.Parser.Nodes;
using Microsoft.WebTools.Languages.Json.VS.SuggestedActions;
using Microsoft.WebTools.Languages.Shared.Parser.Nodes;

namespace Microsoft.Web.LibraryManager.Vsix
{
    [Export(typeof(IJsonSuggestedActionProvider))]
    [Name(nameof(SuggestedActionProvider))]
    internal class SuggestedActionProvider : IJsonSuggestedActionProvider
    {
        public ILibraryInstallationState InstallationState;
        public ObjectNode LibraryObject;
        public string ConfigFilePath;
        public ITextView TextView;
        public ITextBuffer TextBuffer;

        [Import]
        public ITextDocumentFactoryService DocumentService { get; set; }

        [Import]
        public ILibraryCommandService LibraryCommandService { get; set; }

        public IEnumerable<ISuggestedAction> GetSuggestedActions(ITextView textView, ITextBuffer textBuffer, int caretPosition, Node node)
        {
            TextView = textView;
            TextBuffer = textBuffer;

            yield return new UninstallSuggestedAction(this, LibraryCommandService);

            var update = new UpdateSuggestedActionSet(this);

            if (update.HasActionSets)
                yield return update;
        }

        public bool HasSuggestedActions(ITextView textView, ITextBuffer textBuffer, int caretPosition, Node node)
        {
            if (!DocumentService.TryGetTextDocument(textView.TextBuffer, out var doc))
                return false;

            ObjectNode parent = node.FindType<ObjectNode>();

            if (!(parent?.Parent is ArrayElementNode) || !parent.IsValid())
            {
                return false;
            }

            if (!JsonHelpers.TryGetInstallationState(parent, out InstallationState))
            {
                return false;
            }

            ConfigFilePath = doc.FilePath;
            LibraryObject = parent;

            return !string.IsNullOrEmpty(InstallationState.Name);
        }
    }
}
