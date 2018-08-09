using System.Windows.Automation.Peers;
using System.Windows.Controls;
using Microsoft.Web.LibraryManager.Vsix.Resources;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Controls
{
    /// <summary>
    /// Custom AutomationPeer for PackageContentsTreeView control
    /// </summary>
    internal class PackageContentsTreeViewAutomationPeer : UserControlAutomationPeer
    {
        public PackageContentsTreeViewAutomationPeer(UserControl owner) : base(owner)
        {
        }

        protected override string GetLocalizedControlTypeCore()
        {
            return Text.Files;
        }
    }
}
