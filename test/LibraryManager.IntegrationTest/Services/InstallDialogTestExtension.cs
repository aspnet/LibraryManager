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
        public IInstallDialogTestContract InstallDialog
        {
            get
            {
                return this.ObjectUnderTest as IInstallDialogTestContract;
            }
        }

        public void SetLibrary(string library)
        {
            UIInvoke(() =>
            {
                this.InstallDialog.Library = library;
            });
        }

        public void WaitForFileSelections()
        {
            WaitFor.IsTrue(() =>
            {
                return UIInvoke(() =>
                {
                    return this.InstallDialog.IsAnyFileSelected;
                });
            }, TimeSpan.FromSeconds(20), conditionDescription: "File list not loaded");
        }

        public void ClickInstall()
        {
            UIInvoke(() =>
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async () => await this.InstallDialog.ClickInstallAsync()).Task.ConfigureAwait(false);
            });
        }
    }
}
