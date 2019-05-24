// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Models
{
    internal class SelectedProviderBinding : BindableBase
    {
        private IProvider _selectedProvider;

        internal IProvider SelectedProvider
        {
            get { return _selectedProvider; }
            set { Set(ref _selectedProvider, value); }
        }

        internal SelectedProviderBinding()
        {
            _selectedProvider = default(IProvider);
        }
    }
}
