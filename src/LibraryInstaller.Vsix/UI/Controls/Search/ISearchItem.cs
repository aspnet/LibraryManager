using System.Windows.Media;
using Microsoft.Web.LibraryInstaller.Contracts;

namespace Microsoft.Web.LibraryInstaller.Vsix.Controls.Search
{
    public interface ISearchItem
    {
        ILibraryGroup LibraryGroup { get; }

        ImageSource Icon { get; }

        string Homepage { get; }

        string Description { get; }

        string CollapsedItemText { get; }

        string Alias { get; }

        bool IsMatchForSearchTerm(string searchTerm);
    }
}
