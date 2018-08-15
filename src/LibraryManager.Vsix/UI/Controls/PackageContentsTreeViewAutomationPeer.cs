using System.Windows.Automation.Peers;
using System.Windows.Controls;

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

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Tree;
        }
    }
}
