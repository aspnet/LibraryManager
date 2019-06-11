using System;
using System.Globalization;
using System.Windows.Data;
using Microsoft.Web.LibraryManager.Vsix.Resources;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Converters
{
    internal class CheckBoxAutomationNameConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            ValidateInputs(values);

            if (values[2] == null)
            {
                return string.Format(Text.Indeterminate, (string)values[0], (string)values[1]);
            }

            bool isChecked = (bool)values[2];

            if (isChecked)
            {
                return string.Format(Text.Checked, (string)values[0], (string)values[1]);
            }
            else
            {
                return string.Format(Text.UnChecked, (string)values[0], (string)values[1]);
            }
        }

        private void ValidateInputs(object[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (values.Length != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(values), $"{nameof(values)} length must be 3");
            }

            if (!(values[0] is string))
            {
                throw new ArgumentException(string.Format("{0} is not a string", values[0]));
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
