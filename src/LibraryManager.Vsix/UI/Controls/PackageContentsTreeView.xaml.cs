// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.LibraryManager.Vsix.UI.Models;
using Microsoft.Web.LibraryManager.Vsix.UI.Theming;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Controls
{
    /// <summary>
    /// Interaction logic for PackageContentsTreeView.xaml
    /// </summary>
    public partial class PackageContentsTreeView
    {
        public PackageContentsTreeView()
        {
            this.ShouldBeThemed();
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
                TreeView treeView = (TreeView)sender;
                PackageItem packageItem = treeView.SelectedItem as PackageItem;

                if (packageItem != null)
                {
                    if (packageItem.IsChecked.HasValue)
                    {
                        packageItem.IsChecked = !packageItem.IsChecked;
                    }
                    else
                    {
                        // if it is partially checked, clear all selections, just like clicking with the mouse
                        packageItem.IsChecked = false;
                    }
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Tab)
            {
                TreeView treeView = (TreeView)sender;
                TreeViewItem topTreeViewItem = treeView.ItemContainerGenerator.ContainerFromIndex(0) as TreeViewItem;

                if (topTreeViewItem != null)
                {
                    topTreeViewItem.IsSelected = true;
                    e.Handled = true;
                }
            }
        }
    }
}
