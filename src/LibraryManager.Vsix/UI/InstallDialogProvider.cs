using System;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Vsix.UI
{
    /// <summary>
    /// This class gives apex access to opened - add client side libraries dialog.
    /// </summary>
    internal class InstallDialogProvider
    {
        private static IInstallDialog _installDialog;
        public static TaskCompletionSource<bool> WindowIsUp = new TaskCompletionSource<bool>();
        public static IInstallDialog Window
        {
            get { return _installDialog; }
            set
            {
                _installDialog = value;

                if (_installDialog == null)
                {
                    WindowIsUp = new TaskCompletionSource<bool>();
                }
                else
                {
                    WindowIsUp.TrySetResult(true);
                }
            }
        }
    }
}
