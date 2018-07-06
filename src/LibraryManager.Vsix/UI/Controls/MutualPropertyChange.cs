using System.ComponentModel;
using Microsoft.Web.LibraryManager.Vsix.UI.Models;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Controls
{
    internal class MutualPropertyChange : BindableBase
    {
        private string _libraryName;

        internal string TargetLibrary
        {
            get { return _libraryName; }
            set { Set(ref _libraryName, value); }
        }

        private MutualPropertyChange() { }

        internal static MutualPropertyChange Instance { get; } = new MutualPropertyChange();
    }
}
