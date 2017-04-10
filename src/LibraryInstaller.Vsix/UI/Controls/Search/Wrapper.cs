// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Windows;

namespace Microsoft.Web.LibraryInstaller.Vsix.Controls.Search
{
    public class Wrapper : DependencyObject
    {
        public static readonly DependencyProperty ParameterProperty = DependencyProperty.Register(
            "Parameter", typeof(bool), typeof(Wrapper), new PropertyMetadata(default(bool)));

        public bool Parameter
        {
            get { return (bool)GetValue(ParameterProperty); }
            set { SetValue(ParameterProperty, value); }
        }
    }
}