using System;
using System.Windows.Data;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Converters
{
    internal class HintTextConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            IProvider provider = value as IProvider;

            if (value == null || provider == null || provider.LibraryIdHintText == null)
            {
                return string.Empty;
            }

            return provider.LibraryIdHintText;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
