using System.Windows.Automation.Peers;
using System.Windows.Controls;
using Microsoft.Web.LibraryManager.Vsix.Resources;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Controls
{
    /// <summary>
    /// Custom AutomationPeer for TargetLocation control
    /// </summary>
    internal class TargetLocationAutomationPeer : UserControlAutomationPeer
    {
        public TargetLocationAutomationPeer(UserControl owner) : base(owner)
        {
        }

        protected override string GetLocalizedControlTypeCore()
        {
            return Text.TargetLocation;
        }
    }
}
