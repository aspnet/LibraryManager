using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Vsix.UI
{
    /// <summary>
    /// This class gives apex access to opened - add client side libraries dialog.
    /// </summary>
    internal class InstallDialogTestContract
    {
        public static IInstallDialogTestContract Window;
        // The TaskCompletionSource lets apex know when the dialog is open.
        public static TaskCompletionSource<bool> WindowIsUp = new TaskCompletionSource<bool>();
    }
}
