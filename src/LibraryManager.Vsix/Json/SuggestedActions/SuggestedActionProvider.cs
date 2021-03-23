// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Vsix.Contracts;
using Microsoft.Web.LibraryManager.Vsix.Json;
using Microsoft.Web.LibraryManager.Vsix.Shared;
using Microsoft.WebTools.Languages.Json.Parser.Nodes;
using Microsoft.WebTools.Languages.Json.VS.SuggestedActions;
using Microsoft.WebTools.Languages.Shared.Parser.Nodes;

namespace Microsoft.Web.LibraryManager.Vsix.Json.SuggestedActions
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

        [Import]
        public IDependenciesFactory DependenciesFactory { get; private set;}

        public IEnumerable<ISuggestedAction> GetSuggestedActions(ITextView textView, ITextBuffer textBuffer, int caretPosition, Node node)
        {
            TextView = textView;
            TextBuffer = textBuffer;

            yield return new UninstallSuggestedAction(this, LibraryCommandService);

#pragma warning disable CA2000 // Dispose objects before losing scope
            var update = new UpdateSuggestedActionSet(this);
#pragma warning restore CA2000 // Dispose objects before losing scope

            if (update.HasActionSets)
            {
                yield return update;
            }
        }

        public bool HasSuggestedActions(ITextView textView, ITextBuffer textBuffer, int caretPosition, Node node)
        {
            if (!DocumentService.TryGetTextDocument(textView.TextBuffer, out ITextDocument doc))
            {
                return false;
            }

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
