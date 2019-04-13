// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.Test.Apex.VisualStudio;
using Microsoft.Test.Apex.VisualStudio.Shell;
using Microsoft.Web.LibraryManager.Vsix.UI;

namespace Microsoft.Web.LibraryManager.IntegrationTest.Services
{
    /// <summary>
    /// Test service for add client side libraries dialog.
    /// </summary>
    [Export(typeof(InstallDialogTestService))]
    public class InstallDialogTestService : VisualStudioTestService
    {
        public InstallDialogTestExtension OpenDialog()
        {
            var guid = Guid.Parse("44ee7bda-abda-486e-a5fe-4dd3f4cefac1");
            uint commandId = 0x0100;

            _ = Task.Run(() =>
            {
                UIInvoke(() =>
                {
                    CommandingService.ExecuteCommand(guid, commandId, null);
                });
            });

            return WaitForDialog();
        }

        private InstallDialogTestExtension GetInstallDialogTestExtension()
        {
            IInstallDialog addClientSideLibrariesDialogTestContract = InstallDialogProvider.Window;

            if (addClientSideLibrariesDialogTestContract != null)
            {
                return CreateRemotableInstance<InstallDialogTestExtension>(addClientSideLibrariesDialogTestContract);
            }

            return null;
        }

        private InstallDialogTestExtension WaitForDialog()
        {
            if (InstallDialogProvider.Window == null)
            {
                var tcs = new TaskCompletionSource<bool>();
                void onWindowChanged(object sender, EventArgs e)
                {
                    tcs.SetResult(true);
                }

                InstallDialogProvider.WindowChanged += onWindowChanged;
#pragma warning disable VSTHRD102 // Implement internal logic asynchronously
                VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async () => await tcs.Task);
#pragma warning restore VSTHRD102 // Implement internal logic asynchronously

                InstallDialogProvider.WindowChanged -= onWindowChanged;
            }

            InstallDialogTestExtension installDialogExtension = GetInstallDialogTestExtension();

            return installDialogExtension;
        }

        [Import]
        private CommandingService CommandingService
        {
            get;
            set;
        }
    }
}
