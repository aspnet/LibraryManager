using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Test.Apex.Services;
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
            Guid guid = Guid.Parse("44ee7bda-abda-486e-a5fe-4dd3f4cefac1");
            uint commandId = 0x0100;

            _ = VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ExecuteCommandAsync(guid, commandId);
            });

            return WaitForDialog();
        }

        private async Task ExecuteCommandAsync(Guid guid, uint commandId)
        {
            // We don't wait for completion of the command, since this invokes ShowDialog() which is blocking.
            await Task.Factory.StartNew(() =>
            {
                UIInvoke(() =>
                {
                    CommandingService.ExecuteCommand(guid, commandId, null);
                });
            }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default );
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
                TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
                EventHandler onWindowChanged = (object sender, EventArgs e) =>
                {
                    tcs.SetResult(true);
                };

                InstallDialogProvider.WindowChanged += onWindowChanged;
                VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await tcs.Task;
                });

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

        [Import]
        private ISynchronizationService SynchronizationService
        {
            get;
            set;
        }
    }
}
