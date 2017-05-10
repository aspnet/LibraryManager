using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Microsoft.Web.LibraryInstaller.Vsix.UI.Converters
{
    public class BoldingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool && (bool) value)
            {
                return FontWeights.Bold;
            }

            return FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is FontWeight && (FontWeight) value == FontWeights.Bold;
        }
    }
}
