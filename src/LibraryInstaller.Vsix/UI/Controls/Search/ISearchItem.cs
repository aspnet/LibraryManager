using System.Windows.Media;
using Microsoft.Web.LibraryInstaller.Contracts;

namespace Microsoft.Web.LibraryInstaller.Vsix.UI.Controls.Search
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
