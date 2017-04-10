// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Windows;
using Microsoft.Web.LibraryInstaller.Vsix.Models;

namespace Microsoft.Web.LibraryInstaller.Vsix.Controls.Search
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
