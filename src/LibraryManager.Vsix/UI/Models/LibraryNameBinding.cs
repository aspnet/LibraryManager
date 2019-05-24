// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Web.LibraryManager.Vsix.UI.Models
{
    /// <summary>
    /// It will fire an event whenever a valid library name is typed so as to update the target location.
    /// </summary>
    internal class LibraryNameBinding : BindableBase
    {
        private string _libraryName;

        internal string LibraryName
        {
            get { return _libraryName; }
            set { Set(ref _libraryName, value); }
        }

        internal LibraryNameBinding()
        {
            _libraryName = string.Empty;
        }
    }
}
