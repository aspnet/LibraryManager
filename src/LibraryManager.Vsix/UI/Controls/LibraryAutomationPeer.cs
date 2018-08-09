using System.Windows.Automation.Peers;
using System.Windows.Controls;
using Microsoft.Web.LibraryManager.Vsix.Resources;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Controls
{
    /// <summary>
    /// Custom AutomationPeer for Library control
    /// </summary>
    internal class LibraryAutomationPeer : UserControlAutomationPeer
    {
        public LibraryAutomationPeer(UserControl owner) : base(owner)
        {
        }

        protected override string GetLocalizedControlTypeCore()
        {
            return Text.Library;
        }
    }
}
