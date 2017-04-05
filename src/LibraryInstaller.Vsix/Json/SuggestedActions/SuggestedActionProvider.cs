// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.JSON.Core.Parser;
using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.JSON.Editor.Document;
using Microsoft.VisualStudio.JSON.Package.SuggestedActions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using LibraryInstaller.Contracts;

namespace LibraryInstaller.Vsix
{
    [Export(typeof(IJSONSuggestedActionProvider))]
    [Name(nameof(SuggestedActionProvider))]
    internal class SuggestedActionProvider : IJSONSuggestedActionProvider
    {
        private ILibraryInstallationState _installationState;
        private JSONObject _libraryObject;
        private string _configFilePath;

        [Import]
        private ITextDocumentFactoryService DocumentService { get; set; }


        public IEnumerable<ISuggestedAction> GetSuggestedActions(ITextView textView, ITextBuffer textBuffer, int caretPosition, JSONParseItem parseItem)
        {
            yield return new UninstallSuggestedAction(textBuffer, textView, _libraryObject, _installationState.LibraryId, _configFilePath);
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

            if (!JsonHelpers.TryGetInstallationState(parent, out _installationState))
            {
                return false;
            }

            _configFilePath = doc.FilePath;
            _libraryObject = parent;

            return !string.IsNullOrEmpty(_installationState.LibraryId);
        }
    }
}
