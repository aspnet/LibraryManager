// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Controls.AutomationPeers
{
    /// <summary>
    /// Custom AutomationPeer for TargetLocation control
    /// </summary>
    internal class TextControlTypeAutomationPeer : UserControlAutomationPeer
    {
        public TextControlTypeAutomationPeer(UserControl owner) : base(owner)
        {
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Text;
        }
    }
}
