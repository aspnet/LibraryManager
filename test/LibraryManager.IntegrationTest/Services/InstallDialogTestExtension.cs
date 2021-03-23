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

        public string Provider
        {
            get
            {
                return UIInvoke(() => InstallDialog.Provider);
            }
            set
            {
                UIInvoke(() =>
                {
                    InstallDialog.Provider = value;
                });
            }
        }

        public string Library
        {
            get
            {
                return UIInvoke(() => InstallDialog.Library);
            }
            set
            {
                UIInvoke(() =>
                {
                    InstallDialog.Library = value;
                });
            }
        }

        public void WaitForFileSelectionsAvailable()
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
                _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () => await this.InstallDialog.ClickInstallAsync()).Task.ConfigureAwait(false);
            });
        }

        public void Close()
        {
            UIInvoke(() =>
            {
                InstallDialog.CloseDialog();
            });
        }
    }
}
