// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
