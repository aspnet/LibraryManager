using System;
using System.ComponentModel.Composition;
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

            Task.Factory.StartNew(() =>
            {
                UIInvoke(() =>
                {
                    CommandingService.ExecuteCommand(guid, commandId, null);
                });
            });

            return WaitForDialog(TimeSpan.FromSeconds(5));
        }

        [Import]
        private ISynchronizationService SynchronizationService
        {
            get;
            set;
        }

        private InstallDialogTestExtension GetInstallDialogTestExtension()
        {
            IInstallDialogTestContract addClientSideLibrariesDialogTestContract = InstallDialogTestContract.Window;

            if (addClientSideLibrariesDialogTestContract != null)
            {
                return CreateRemotableInstance<InstallDialogTestExtension>(addClientSideLibrariesDialogTestContract);
            }

            return null;
        }

        private InstallDialogTestExtension WaitForDialog(TimeSpan timeout)
        {
            InstallDialogTestExtension installDialogExtension = GetInstallDialogTestExtension();

            if (!InstallDialogTestContract.WindowIsUp.WaitOne(TimeSpan.FromMilliseconds(timeout.TotalMilliseconds * SynchronizationService.TimeoutMultiplier)))
            {
                throw new TimeoutException("Add -> Client Side Libraries dialog didn't pop up");
            }

            installDialogExtension = GetInstallDialogTestExtension();

            return installDialogExtension;
        }

        [Import(AllowDefault = true)]
        private Lazy<CommandingService> LazyCommandingService
        {
            get;
            set;
        }

        private CommandingService CommandingService
        {
            get
            {
                return LazyCommandingService.Value;
            }
        }
    }
}
