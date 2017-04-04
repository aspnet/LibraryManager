// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using LibraryInstaller.Contracts;
using LibraryInstaller.Vsix.Controls.Search;
using Microsoft.VisualStudio.Imaging;

namespace LibraryInstaller.Vsix.Models
{
    internal class PackageSearchItem : BindableBase, ISearchItem
    {
        private readonly Dispatcher _dispatcher;
        private string _description;
        private string _homepage;
        private ImageSource _icon;
        private Lazy<Task<string>> _infoTask;
        private static ConcurrentDictionary<string, PackageSearchItem> _cache = new ConcurrentDictionary<string, PackageSearchItem>();
        private bool _special;
        private static PackageSearchItem _missing;

        public static PackageSearchItem Missing
        {
            get { return _missing ?? (_missing = new PackageSearchItem()); }
        }

        public static PackageSearchItem GetOrCreate(IProvider provider, string name, string alias = null)
        {
            return _cache.GetOrAdd(name, n => new PackageSearchItem(provider, n, alias));
        }

        private PackageSearchItem()
        {
            _special = true;
            CollapsedItemText = Resources.Text.PackagesCouldNotBeLoaded;
        }

        private PackageSearchItem(IProvider provider, string name, string alias = null)
        {
            Alias = alias ?? name;
            _dispatcher = Dispatcher.CurrentDispatcher;
            CollapsedItemText = name;
            Icon = WpfUtil.GetIconForImageMoniker(KnownMonikers.Package, 24, 24);
            _infoTask = new Lazy<Task<string>>(async () =>
            {
                ILibraryCatalog catalog = provider.GetCatalog();
                IReadOnlyList<ILibraryGroup> packageGroups = await catalog.SearchAsync(name, 1, CancellationToken.None).ConfigureAwait(false);
                IEnumerable<string> displayInfos = await packageGroups[0].GetLibraryIdsAsync(CancellationToken.None).ConfigureAwait(false);
                return displayInfos.FirstOrDefault();
            });
        }

        public string CollapsedItemText { get; }

        public bool IsMatchForSearchTerm(string searchTerm)
        {
            return PackageSearchUtil.ForTerm(searchTerm).IsMatch(this);
        }

        public string Name => CollapsedItemText;

        public string Description
        {
            get
            {
                if (!_special && !_infoTask.Value.IsCompleted)
                {
                    LoadPackageInfoAsync();

                    if (!_infoTask.Value.IsCompleted)
                    {
                        return Resources.Text.Loading;
                    }
                }

                return _description;
            }
            set { Set(ref _description, value); }
        }

        public string Homepage
        {
            get { return _homepage; }
            set { Set(ref _homepage, value); }
        }

        public ImageSource Icon
        {
            get { return _icon; }
            set { Set(ref _icon, value); }
        }

        public string Alias { get; }

        private /*async*/ void LoadPackageInfoAsync()
        {
            //IPackageDisplayInfo info = await _infoTask.Value.ConfigureAwait(false);

            //await _dispatcher.InvokeAsync(() =>
            //{
            //    Description = info.Description;
            //    Homepage = info.Homepage;

            //    if (info.Icon != null)
            //    {
            //        Icon = info.Icon;
            //    }
            //});
        }
    }
}
