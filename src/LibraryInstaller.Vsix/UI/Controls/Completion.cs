using Microsoft.Web.LibraryInstaller.Contracts;

namespace Microsoft.Web.LibraryInstaller.Vsix.UI.Controls
{
    public class Completion
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
