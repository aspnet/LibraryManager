using System.ComponentModel;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Controls
{
    internal class MutualPropertyChange : INotifyPropertyChanged
    {
        private string _libraryName;
        public event PropertyChangedEventHandler PropertyChanged;

        internal string TargetLibrary
        {
            get { return _libraryName; }
            set
            {
                _libraryName = value;
                OnPropertyChanged(_libraryName);
            }
        }

        private MutualPropertyChange() { }

        internal static MutualPropertyChange Instance { get; } = new MutualPropertyChange();

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
        
    }
}
