// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Converters
{
    public class WatermarkVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool shouldShow = value is null;
            if (value is string s)
            {
                shouldShow = string.IsNullOrEmpty(s);
            }

            return shouldShow ? Visibility.Visible : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
