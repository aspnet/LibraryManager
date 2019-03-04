using System;
using System.Globalization;
using System.Windows.Data;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Converters
{
    internal class CheckBoxAutomationNameConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 2 || !(values[0] is string) || !(values[1] is bool))
            {
                return null;
            }

            bool isChecked = (bool)values[1];

            if (isChecked)
            {
                return "Checked" + (string)values[0];
            }
            else
            {
                return "UnChecked" + (string)values[0];
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
