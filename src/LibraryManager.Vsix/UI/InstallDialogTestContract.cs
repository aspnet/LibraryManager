using System.Threading;

namespace Microsoft.Web.LibraryManager.Vsix.UI
{
    /// <summary>
    /// This class gives apex access to opened - add client side libraries dialog.
    /// </summary>
    internal class InstallDialogTestContract
    {
        public static IInstallDialogTestContract window;
        // This event lets apex know when the dialog is open.
        public static EventWaitHandle windowIsUp = new EventWaitHandle(false, EventResetMode.ManualReset);
    }
}
