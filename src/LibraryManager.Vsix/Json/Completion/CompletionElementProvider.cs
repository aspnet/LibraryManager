// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows;

namespace Microsoft.Web.LibraryManager.Vsix.Json.Completion
{
    [Export(typeof(IUIElementProvider<VisualStudio.Language.Intellisense.Completion, ICompletionSession>))]
    [Name(nameof(CompletionElementProvider))]
    [ContentType("JSON")]
    internal class CompletionElementProvider : IUIElementProvider<VisualStudio.Language.Intellisense.Completion, ICompletionSession>
    {
        public UIElement GetUIElement(VisualStudio.Language.Intellisense.Completion itemToRender, ICompletionSession context, UIElementType elementType)
        {
            if (elementType == UIElementType.Tooltip && itemToRender is SimpleCompletionEntry entry)
            {
                if (!string.IsNullOrEmpty(entry.Description))
                {
                    return new UI.Controls.EditorTooltip(entry);
                }
            }

            return null;
        }
    }
}
