using System;
using System.ComponentModel;
using System.Windows;
using LibraryInstaller.Vsix.Models;

namespace LibraryInstaller.Vsix.Controls.Search
{
    public class SearchItemContainer : BindableBase
    {
        private bool _isSelected;
        private readonly ComboSearch _search;

        public SearchItemContainer(ComboSearch search, ISearchItem item)
        {
            Item = item;
            _search = search;
            search.SearchTextChanged += OnSearchTextChanged;
        }

        private void OnSearchTextChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(SearchText));
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (Set(ref _isSelected, value))
                {
                    TemplateChanged();
                }
            }
        }

        public string SearchText => _search.Text;

        public void TemplateChanged()
        {
            OnPropertyChanged(nameof(ItemTemplate));
        }

        public ISearchItem Item { get; }

        public DataTemplate ItemTemplate => _search.SelectedItem == Item || _search.SelectedItem == null && _search.ItemsSource.IndexOf(Item) == 0 ? _search.ExpandedTemplate : _search.CollapsedTemplate;
    }
}
