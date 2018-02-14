using System.Windows.Media;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Controls.Search
{
    public interface ISearchItem
    {
        string Alias { get; }

        string CollapsedItemText { get; }

        string Description { get; }

        string Homepage { get; }

        ImageSource Icon { get; }

        ILibraryGroup LibraryGroup { get; }

        bool IsMatchForSearchTerm(string searchTerm);
    }
}
