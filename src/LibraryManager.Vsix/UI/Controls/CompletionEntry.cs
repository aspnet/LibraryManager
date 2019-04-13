// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Controls
{
    public class CompletionEntry
    {
        public CompletionEntry(CompletionItem completionItem, int start, int length)
        {
            Start = start;
            Length = length;
            CompletionItem = completionItem;
        }

        public CompletionItem CompletionItem { get; }

        public string Description => CompletionItem.Description;

        public string DisplayText => CompletionItem.DisplayText;

        public int Length { get; }

        public int Start { get; }
    }
}
