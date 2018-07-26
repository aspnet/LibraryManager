using System.Threading;

namespace Microsoft.Web.LibraryManager.Vsix.UI
{
    /// <summary>
    /// This class gives apex access to opened - add client side libraries dialog.
    /// </summary>
    internal class InstallDialogTestContract
    {
        public static IInstallDialogTestContract Window;
        // This event lets apex know when the dialog is open.
        public static EventWaitHandle WindowIsUp = new EventWaitHandle(false, EventResetMode.ManualReset);
    }
}
