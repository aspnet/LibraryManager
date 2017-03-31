using System;
using System.Globalization;
using System.Windows.Data;

namespace LibraryInstaller.Vsix.Controls.Search
{
    public class LogicalOrConverter : IMultiValueConverter
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
                if (values[i] is bool && (bool)values[i])
                {
                    return true;
                }
            }

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
