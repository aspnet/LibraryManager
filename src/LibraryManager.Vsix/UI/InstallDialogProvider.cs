using System;

namespace Microsoft.Web.LibraryManager.Vsix.UI
{
    /// <summary>
    /// This class gives apex access to opened - add client side libraries dialog.
    /// </summary>
    internal class InstallDialogProvider
    {
        private static IInstallDialog _installDialog;
        public static event EventHandler WindowChanged;

        public static IInstallDialog Window
        {
            get { return _installDialog; }
            set
            {
                _installDialog = value;

                WindowChanged?.Invoke(null, new EventArgs());
            }
        }
    }
}
