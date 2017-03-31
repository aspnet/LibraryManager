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