// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Vsix.UI
{
    /// <summary>
    /// Test contract for add client side libraries dialog.
    /// </summary>
    public interface IInstallDialog
    {
        string Library { get; set; }

        Task ClickInstallAsync();

        bool IsAnyFileSelected { get; }
    }
}
