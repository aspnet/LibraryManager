using System.ComponentModel;
using Microsoft.Web.LibraryManager.Vsix.UI.Models;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Controls
{
    /// <summary>
    /// It will fire an event whenever a valid library name is typed so as to update the target location.
    /// </summary>
    internal class BindLibraryNameToTargetLocation : BindableBase
    {
        private string _libraryName;

        internal string LibraryName
        {
            get { return _libraryName; }
            set { Set(ref _libraryName, value); }
        }

        internal BindLibraryNameToTargetLocation()
        {
            _libraryName = string.Empty;
        }
    }
}
