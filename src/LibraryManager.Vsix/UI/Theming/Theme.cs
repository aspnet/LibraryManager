using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Theming
{
    internal static class Theme
    {
        private static ResourceDictionary ThemeResources { get; } = BuildThemeResources();

        public static void ShouldBeThemed(this FrameworkElement control)
        {
            if (control.Resources == null)
            {
                control.Resources = ThemeResources;
            }
            else if (control.Resources != ThemeResources)
            {
                ResourceDictionary d = new ResourceDictionary();
                d.MergedDictionaries.Add(ThemeResources);
                d.MergedDictionaries.Add(control.Resources);
                control.Resources = null;
                control.Resources = d;
            }

            // This makes sure that the ImageMonikers are rendered with the correct background color
            control.SetResourceReference(ImageThemingUtilities.ImageBackgroundColorProperty, "VsColor.Window");
        }

        private static ResourceDictionary BuildThemeResources()
        {
            ResourceDictionary allResources = new ResourceDictionary();
            ResourceDictionary shellResources = (ResourceDictionary) Application.LoadComponent(new Uri("Microsoft.VisualStudio.Platform.WindowManagement;component/Themes/ThemedDialogDefaultStyles.xaml", UriKind.Relative));
            ResourceDictionary scrollStyleContainer = (ResourceDictionary) Application.LoadComponent(new Uri("Microsoft.VisualStudio.Shell.UI.Internal;component/Styles/ScrollBarStyle.xaml", UriKind.Relative));
            ResourceDictionary localThemingContainer = (ResourceDictionary)Application.LoadComponent(new Uri("Microsoft.Web.LibraryManager.Vsix;component/UI/Theming/Shared.xaml", UriKind.Relative));
            ResourceDictionary comboTheme = (ResourceDictionary)Application.LoadComponent(new Uri("Microsoft.Web.LibraryManager.Vsix;component/UI/Theming/VsThemedComboBox.xaml", UriKind.Relative));
            ResourceDictionary tooltipTheme = (ResourceDictionary)Application.LoadComponent(new Uri("Microsoft.Web.LibraryManager.Vsix;component/UI/Theming/VsThemedToolTip.xaml", UriKind.Relative));
            ResourceDictionary treeViewTheme = (ResourceDictionary)Application.LoadComponent(new Uri("Microsoft.Web.LibraryManager.Vsix;component/UI/Theming/VsThemedTreeView.xaml", UriKind.Relative));
            allResources.MergedDictionaries.Add(shellResources);
            allResources.MergedDictionaries.Add(scrollStyleContainer);
            allResources.MergedDictionaries.Add(localThemingContainer);
            allResources.MergedDictionaries.Add(comboTheme);
            allResources.MergedDictionaries.Add(tooltipTheme);
            allResources.MergedDictionaries.Add(treeViewTheme);
            allResources[typeof (ScrollViewer)] = new Style
            {
                TargetType = typeof (ScrollViewer),
                BasedOn = (Style) scrollStyleContainer[VsResourceKeys.ScrollViewerStyleKey]
            };
            return allResources;
        }
    }
}
