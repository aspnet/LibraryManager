// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Web.LibraryInstaller.Contracts;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.Web.LibraryInstaller.Vsix.Models
{
    public class InstallDialogViewModel : BindableBase
    {
        private readonly Action<bool> _closeDialog;
        //private IReadOnlyList<ILibraryDisplayInfo> _availablePackages;
        private readonly Dispatcher _dispatcher;

        public InstallDialogViewModel(Dispatcher dispatcher, Action<bool> closeDialog)
        {
            _dispatcher = dispatcher;
            _closeDialog = closeDialog;
            InstallPackageCommand = ActionCommand.Create(InstallPackage, CanInstallPackage, false);
            Task t = LoadPackagesAsync();
        }

        private Task LoadPackagesAsync()
        {
            throw new NotImplementedException();
        }

        public ICommand InstallPackageCommand { get; }

        private bool CanInstallPackage()
        {
            throw new NotImplementedException();
        }

        private void InstallPackage()
        {
            throw new NotImplementedException();
            //_closeDialog(true);
        }
    }
}
