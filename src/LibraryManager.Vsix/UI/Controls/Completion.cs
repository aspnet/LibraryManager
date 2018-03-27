using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Controls
{
    internal class Completion
    {
        public Completion(CompletionItem completionItem, int start, int length)
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
