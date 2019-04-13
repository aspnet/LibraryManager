// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.LibraryManager.Vsix.UI.Models;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Controls
{
    /// <summary>
    /// Interaction logic for PackageContentsTreeView.xaml
    /// </summary>
    public partial class PackageContentsTreeView
    {
        public PackageContentsTreeView()
        {
            InitializeComponent();
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new PackageContentsTreeViewAutomationPeer(this);
        }

        private void OnPreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                var treeView = (TreeView)sender;

                if (treeView.SelectedItem is PackageItem packageItem)
                {
                    packageItem.IsChecked = !packageItem.IsChecked;
                    e.Handled = true;
                }
            }
        }
    }
}
