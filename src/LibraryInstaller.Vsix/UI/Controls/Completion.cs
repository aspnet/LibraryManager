using Microsoft.Web.LibraryInstaller.Contracts;

namespace Microsoft.Web.LibraryInstaller.Vsix.Controls
{
    public class Completion
    {
        public Completion(CompletionItem completionItem, int start, int length)
        {
            Start = start;
            Length = length;
            CompletionItem = completionItem;
        }

        public int Start { get; }

        public int Length { get; }

        public CompletionItem CompletionItem { get; }

        public string Description => CompletionItem.Description;

        public string DisplayText => CompletionItem.DisplayText;
    }
}
