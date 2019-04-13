// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Windows.Data;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Converters
{
    internal class EnumToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                return value.ToString().Equals(parameter);
            }

            return false;         
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return Binding.DoNothing;
            }

            return value.Equals(true) ? parameter : Binding.DoNothing;
        }
    }
}
