using System.Windows.Media;
using System.Windows.Media.Imaging;
using LibraryManager.Contracts;
using LibraryManager.Vsix.Models;

namespace LibraryManager.Vsix.Controls.Search
{
    internal class LibraryGroupToSearchItemAdapter : ISearchItem
    {
        private ILibraryGroup _source;

        public LibraryGroupToSearchItemAdapter(ILibraryGroup source)
        {
            _source = source;
        }

        public string CollapsedItemText => _source.Name;

        public string Alias => _source.Name;

        public ImageSource Icon => null;

        public string Homepage => "N/A";

        public string Description => _source.Description;

        public ILibraryGroup LibraryGroup => _source;

        public bool IsMatchForSearchTerm(string searchTerm)
        {
            return PackageSearchUtil.ForTerm(searchTerm).IsMatch(this);
        }
    }
}