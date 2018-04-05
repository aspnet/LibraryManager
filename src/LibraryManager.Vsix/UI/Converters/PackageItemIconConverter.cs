using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.Web.LibraryManager.Vsix.UI.Models;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Converters
{
    internal class PackageItemIconConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 6 || !(values[0] is PackageItemType) || !(values[1] is string) || !(values[2] is bool) || !(values[3] is double) || !(values[4] is double) || !(values[5] is DependencyObject))
            {
                return null;
            }

            int x = (int)(double) values[3];
            int y = (int)(double) values[4];

            if (x < 1)
            {
                x = 1;
            }

            if (y < 1)
            {
                y = 1;
            }

            PackageItemType type = (PackageItemType) values[0];
            if (type == PackageItemType.Folder)
            {
                bool isExpanded = (bool) values[2];
                ImageMoniker moniker = isExpanded ? KnownMonikers.FolderOpened : KnownMonikers.FolderClosed;
                return WpfUtil.ThemeImage((DependencyObject)values[5], WpfUtil.GetIconForImageMoniker(moniker, x, y));
            }

            string name = (string) values[1];
            bool isThemeIcon;
            ImageSource source = WpfUtil.GetIconForFile((DependencyObject)values[5], name, out isThemeIcon);
            return source;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}