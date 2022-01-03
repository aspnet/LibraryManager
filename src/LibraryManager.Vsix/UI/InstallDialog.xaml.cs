// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Vsix.UI.Models;
using Microsoft.Web.LibraryManager.Vsix.UI.Theming;
using Shell = Microsoft.VisualStudio.Shell;

namespace Microsoft.Web.LibraryManager.Vsix.UI
{
    internal partial class InstallDialog : ThemedWindow, IInstallDialog
    {
        public InstallDialog(InstallDialogViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            Loaded += OnLoaded;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            OnActivateTestContract();
        }

        private void OnActivateTestContract()
        {
            InstallDialogProvider.Window = this;
        }

        protected override void OnClosed(EventArgs e)
        {
            InstallDialogProvider.Window = null;
        }

        internal InstallDialogViewModel ViewModel
        {
            get { return DataContext as InstallDialogViewModel; }
            set { DataContext = value; }
        }

        private void CloseDialog(bool res)
        {
            try
            {
                DialogResult = res;
            }
            catch (InvalidOperationException)
            { }

            Close();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            FocusManager.SetFocusedElement(ProviderComboBox, ProviderComboBox);
        }

        private void ProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // User chose to reset provider. So we need to reset all controls to initial state.
            if (!ViewModel.IsTreeViewEmpty)
            {
                IncludeAllLibraryFilesRb.IsChecked = true;
                ViewModel.LibraryIdViewModel.SearchText = string.Empty;
                ViewModel.IsTreeViewEmpty = true;
                ViewModel.LibraryId = null;
                ViewModel.AnyFileSelected = false;
            }
        }

        private void InstallButton_Clicked(object sender, RoutedEventArgs e)
        {
            Shell.ThreadHelper.JoinableTaskFactory.RunAsync(() => ClickInstallButtonAsync());
        }

        private async Task<bool> IsLibraryInstallationStateValidAsync()
        {
            bool isLibraryInstallationStateValid = await ViewModel.IsLibraryInstallationStateValidAsync().ConfigureAwait(false);
            return isLibraryInstallationStateValid;
        }

        async Task ClickInstallButtonAsync()
        {
            bool isLibraryInstallationStateValid = await IsLibraryInstallationStateValidAsync().ConfigureAwait(false);
            await Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (isLibraryInstallationStateValid)
            {
                CloseDialog(true);
                ViewModel.InstallPackageCommand.Execute(null);
            }
            else
            {
                var shell = Shell.Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;
                shell.ShowMessageBox(dwCompRole: 0,
                                     rclsidComp: Guid.Empty,
                                     pszTitle: null,
                                     pszText: ViewModel.ErrorMessage,
                                     pszHelpFile: null,
                                     dwHelpContextID: 0,
                                     msgbtn: OLEMSGBUTTON.OLEMSGBUTTON_OK,
                                     msgdefbtn: OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                                     msgicon: OLEMSGICON.OLEMSGICON_WARNING,
                                     fSysAlert: 0,
                                     pnResult: out _);
            }
        }

        string IInstallDialog.Provider
        {
            get
            {
                return ViewModel.SelectedProvider.Id;
            }
            set
            {
                IProvider intendedProvider = ViewModel.Providers.SingleOrDefault(p => p.Id == value);
                if (intendedProvider == null)
                {
                    throw new InvalidOperationException($"A provider named {value} could not be selected");
                }

                ViewModel.SelectedProvider = intendedProvider;
            }
        }

        string IInstallDialog.Library
        {
            get
            {
                return ViewModel.LibraryIdViewModel.SearchText;
            }
            set
            {
                ViewModel.LibraryIdViewModel.SearchText = value;
            }
        }

        async Task IInstallDialog.ClickInstallAsync()
        {
            await ClickInstallButtonAsync();
        }

        void IInstallDialog.CloseDialog()
        {
            CloseDialog(false);
        }

        bool IInstallDialog.IsAnyFileSelected
        {
            get
            {
                return !ViewModel.IsTreeViewEmpty;
            }
        }
    }
}
