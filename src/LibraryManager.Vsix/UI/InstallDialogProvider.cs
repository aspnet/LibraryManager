// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Web.LibraryManager.Vsix.UI
{
    /// <summary>
    /// This class gives apex access to opened - add client side libraries dialog.
    /// </summary>
    internal class InstallDialogProvider
    {
        private static IInstallDialog InstallDialog;
        public static event EventHandler WindowChanged;

        public static IInstallDialog Window
        {
            get { return InstallDialog; }
            set
            {
                InstallDialog = value;

                WindowChanged?.Invoke(null, new EventArgs());
            }
        }
    }
}
