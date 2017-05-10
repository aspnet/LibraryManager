using System;
using System.Globalization;
using System.Windows.Data;

namespace Microsoft.Web.LibraryInstaller.Vsix.UI.Controls.Search
{
    public class LogicalAndConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Wrapper wrapper = parameter as Wrapper;

            if (wrapper != null && wrapper.Parameter)
            {
                return true;
            }

            for (int i = 0; i < values.Length; ++i)
            {
                if (!(values[i] is bool b) || !b)
                {
                    return false;
                }
            }

            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}