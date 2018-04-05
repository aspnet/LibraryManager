using System;
using System.Globalization;
using System.Windows.Data;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Converters
{
    internal class NotConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(value is bool && (bool) value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(value is bool && (bool)value);
        }
    }
}