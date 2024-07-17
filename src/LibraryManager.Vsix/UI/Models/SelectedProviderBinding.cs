// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
