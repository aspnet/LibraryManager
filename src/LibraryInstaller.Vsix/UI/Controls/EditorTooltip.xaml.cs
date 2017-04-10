// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.PlatformUI;
using System.Windows.Controls;

namespace Microsoft.Web.LibraryInstaller.Vsix
{
    /// <summary>
    /// Interaction logic for EditorTooltip.xaml
    /// </summary>
    public partial class EditorTooltip : UserControl
    {
        private const int _iconSize = 32;

        internal EditorTooltip(SimpleCompletionEntry item)
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                ItemName.Content = item.DisplayText;
                ItemName.SetResourceReference(TextBlock.ForegroundProperty, EnvironmentColors.SystemMenuTextBrushKey);

                Description.Text = item.Description;
                Description.SetResourceReference(TextBlock.ForegroundProperty, EnvironmentColors.SystemMenuTextBrushKey);

                Glyph.Source = WpfUtil.GetIconForImageMoniker(item.IconMoniker, _iconSize, _iconSize);
            };
        }
    }
}
