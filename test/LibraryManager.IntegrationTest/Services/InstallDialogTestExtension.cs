// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Test.Apex.Services;
using Microsoft.Test.Apex.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.Web.LibraryManager.Vsix.UI;

namespace Microsoft.Web.LibraryManager.IntegrationTest.Services
{
    public class InstallDialogTestExtension : VisualStudioInProcessTestExtension<object, InstallDialogVerifier>
    {
        /// <summary>
        /// Add client side libraries dialog test extension for interaction with visual studio inprocess types
        /// </summary>
        public IInstallDialog InstallDialog
        {
            get
            {
                return ObjectUnderTest as IInstallDialog;
            }
        }

        public void SetLibrary(string library)
        {
            UIInvoke(() =>
            {
                InstallDialog.Library = library;
            });

            WaitForFileSelections();
        }

        private void WaitForFileSelections()
        {
            WaitFor.IsTrue(() =>
            {
                return UIInvoke(() =>
                {
                    return InstallDialog.IsAnyFileSelected;
                });
            }, TimeSpan.FromSeconds(20), conditionDescription: "File list not loaded");
        }

        public void ClickInstall()
        {
            UIInvoke(() =>
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async () => await InstallDialog.ClickInstallAsync()).Task.ConfigureAwait(false);
            });
        }
    }
}
