// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryInstaller.Contracts;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Web.LibraryInstaller
{
    internal class LibraryInstallationResult : ILibraryInstallationResult
    {
        public LibraryInstallationResult(ILibraryInstallationState installationState)
        {
            Errors = new List<IError>();
            InstallationState = installationState;
        }

        public LibraryInstallationResult(ILibraryInstallationState installationState, params IError[] error)
        {
            var list = new List<IError>();
            list.AddRange(error);
            Errors = list;
            InstallationState = installationState;
        }

        public bool Cancelled { get; set; }

        public bool Success
        {
            get { return !Cancelled && Errors.Count == 0; }
        }

        public IList<IError> Errors { get; set; }

        public ILibraryInstallationState InstallationState { get; set; }

        public static LibraryInstallationResult FromSuccess(ILibraryInstallationState installationState)
        {
            return new LibraryInstallationResult(installationState);
        }

        public static LibraryInstallationResult FromCancelled(ILibraryInstallationState installationState)
        {
            return new LibraryInstallationResult(installationState)
            {
                Cancelled = true
            };
        }
    }
}
