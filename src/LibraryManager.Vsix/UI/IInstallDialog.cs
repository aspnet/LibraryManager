// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Vsix.UI
{
    /// <summary>
    /// Test contract for add client side libraries dialog.
    /// </summary>
    public interface IInstallDialog
    {
        string Provider { get; set; }

        string Library { get; set; }

        Task ClickInstallAsync();

        void CloseDialog();

        bool IsAnyFileSelected { get; }
    }
}
