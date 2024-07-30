// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
