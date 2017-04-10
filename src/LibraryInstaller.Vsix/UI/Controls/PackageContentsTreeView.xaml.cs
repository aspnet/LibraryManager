// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Windows;
using System.Windows.Data;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.Web.LibraryInstaller.Vsix.Controls
{
    /// <summary>
    /// Interaction logic for PackageContentsTreeView.xaml
    /// </summary>
    public partial class PackageContentsTreeView
    {
        public PackageContentsTreeView()
        {
            InitializeComponent();
            SetBinding(ImageThemingUtilities.ImageBackgroundColorProperty, new Binding
            {
                Source = Content,
                Path = new PropertyPath("Background"),
                Converter = new BrushToColorConverter()
            });
        }
    }
}
