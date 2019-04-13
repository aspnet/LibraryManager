// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Windows.Data;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Controls.Search
{
    internal class LogicalOrConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is Wrapper wrapper && wrapper.Parameter)
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
