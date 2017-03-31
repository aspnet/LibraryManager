// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace LibraryInstaller.Vsix
{
    [Export(typeof(ISuggestedActionsSourceProvider))]
    [Name(nameof(LibraryInstallerSuggestedActionsSourceProvider))]
    [ContentType("JSON")]
    class LibraryInstallerSuggestedActionsSourceProvider : ISuggestedActionsSourceProvider
    {
        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer buffer)
        {
            return textView.Properties.GetOrCreateSingletonProperty(() => new SuggestedActionsSource(textView, buffer));
        }
    }
}
