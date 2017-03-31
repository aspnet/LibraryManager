using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows;

namespace LibraryInstaller.Vsix
{
    [Export(typeof(IUIElementProvider<Completion, ICompletionSession>))]
    [Name(nameof(CompletionElementProvider))]
    [ContentType("JSON")]
    public class CompletionElementProvider : IUIElementProvider<Completion, ICompletionSession>
    {
        public UIElement GetUIElement(Completion itemToRender, ICompletionSession context, UIElementType elementType)
        {
            if (elementType == UIElementType.Tooltip && itemToRender is SimpleCompletionEntry entry)
            {
                if (!string.IsNullOrEmpty(entry.Description))
                {
                    return new EditorTooltip(entry);
                }
            }

            return null;
        }
    }
}
