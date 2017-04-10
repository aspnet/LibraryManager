// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Web.LibraryInstaller.Vsix.Models;

namespace Microsoft.Web.LibraryInstaller.Vsix
{
    public partial class InstallDialog
    {
        private readonly string _folder;

        public InstallDialog(string folder)
        {
            InitializeComponent();

            _folder = folder;

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Icon = BitmapFrame.Create(new Uri("pack://application:,,,/LibraryInstaller.Vsix;component/Resources/dialog-icon.png", UriKind.RelativeOrAbsolute));
            Title = Vsix.Name;

            cbName.Focus();

            ViewModel = new InstallDialogViewModel(Dispatcher, CloseDialog);
        }

        private void CloseDialog(bool res)
        {
            DialogResult = res;
            Close();
        }

        public InstallDialogViewModel ViewModel
        {
            get { return DataContext as InstallDialogViewModel; }
            set { DataContext = value; }
        }

        private void NavigateToHomepage(object sender, RequestNavigateEventArgs e)
        {
            Hyperlink link = sender as Hyperlink;

            if (link != null)
            {
                Process.Start(link.NavigateUri.AbsoluteUri);
            }

            e.Handled = true;
            cbName.ResumeFocusEvents();
        }

        private void HyperlinkPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            cbName.SuspendFocusEvents();
        }
    }
}
