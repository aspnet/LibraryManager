// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace LibraryInstaller.Vsix.Models
{
    public class PackageVersion : BindableBase
    {
        private bool _isStable;
        private string _value;

        public bool IsStable
        {
            get { return _isStable; }
            set { Set(ref _isStable, value); }
        }

        public string Value
        {
            get { return _value; }
            set { Set(ref _value, value, StringComparer.Ordinal); }
        }
    }
}