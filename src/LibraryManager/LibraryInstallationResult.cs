// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager
{
    /// <summary>Internal use only</summary>
    internal class LibraryInstallationResult : ILibraryInstallationResult
    {
        /// <summary>Internal use only</summary>
        public LibraryInstallationResult(ILibraryInstallationState installationState)
        {
            Errors = new List<IError>();
            InstallationState = installationState;
        }

        /// <summary>Internal use only</summary>
        public LibraryInstallationResult(ILibraryInstallationState installationState, params IError[] error)
        {
            var list = new List<IError>();
            list.AddRange(error);
            Errors = list;
            InstallationState = installationState;
        }

        /// <summary>Internal use only</summary>
        public LibraryInstallationResult(params IError[] error)
        {
            Errors = new List<IError>();
            foreach (IError e in error)
            {
                Errors.Add(e);
            }
        }

        /// <summary>
        /// <code>True</code> if the installation was cancelled; otherwise false;
        /// </summary>
        public bool Cancelled { get; set; }

        /// <summary>
        /// <code>True</code> if the library is up to date; otherwise false;
        /// </summary>
        public bool UpToDate { get; set; }

        /// <summary>
        /// <code>True</code> if the install was successfull; otherwise <code>False</code>.
        /// </summary>
        /// <remarks>
        /// The value is usually <code>True</code> if the <see cref="P:Microsoft.Web.LibraryManager.Contracts.ILibraryInstallationResult.Errors" /> list is empty.
        /// </remarks>
        public bool Success
        {
            get { return !Cancelled && Errors.Count == 0; }
        }

        /// <summary>
        /// A list of errors that occured during library installation.
        /// </summary>
        public IList<IError> Errors { get; set; }

        /// <summary>
        /// The <see cref="T:Microsoft.Web.LibraryManager.Contracts.ILibraryInstallationState" /> object passed to the
        /// <see cref="T:Microsoft.Web.LibraryManager.Contracts.IProvider" /> for installation.
        /// </summary>
        public ILibraryInstallationState InstallationState { get; set; }

        /// <summary>Internal use only</summary>
        public static LibraryInstallationResult FromSuccess(ILibraryInstallationState installationState)
        {
            return new LibraryInstallationResult(installationState);
        }

        /// <summary>Internal use only</summary>
        public static LibraryInstallationResult FromCancelled(ILibraryInstallationState installationState)
        {
            return new LibraryInstallationResult(installationState)
            {
                Cancelled = true
            };
        }

        /// <summary>Internal use only</summary>
        public static LibraryInstallationResult FromError(IError error)
        {
            return new LibraryInstallationResult(error);
        }

        /// <summary>Internal use only</summary>
        public static ILibraryInstallationResult FromUpToDate(ILibraryInstallationState installationState)
        {
            return new LibraryInstallationResult(installationState)
            {
                UpToDate = true
            };
        }
    }
}
