// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager
{
    /// <summary>Internal use only</summary>
    internal class LibraryOperationResult : ILibraryOperationResult
    {
        /// <summary>Internal use only</summary>
        public LibraryOperationResult(ILibraryInstallationState installationState)
        {
            Errors = new List<IError>();
            InstallationState = installationState;
        }

        /// <summary>Internal use only</summary>
        public LibraryOperationResult(ILibraryInstallationState installationState, params IError[] error)
        {
            var list = new List<IError>();
            list.AddRange(error);
            Errors = list;
            InstallationState = installationState;
        }

        /// <summary>Internal use only</summary>
        public LibraryOperationResult(params IError[] error)
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
        /// The value is usually <code>True</code> if the <see cref="Microsoft.Web.LibraryManager.Contracts.ILibraryOperationResult.Errors" /> list is empty.
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
        /// The <see cref="Microsoft.Web.LibraryManager.Contracts.ILibraryInstallationState" /> object passed to the
        /// <see cref="Microsoft.Web.LibraryManager.Contracts.IProvider" /> for installation.
        /// </summary>
        public ILibraryInstallationState InstallationState { get; set; }

        /// <summary>Internal use only</summary>
        public static LibraryOperationResult FromSuccess(ILibraryInstallationState installationState)
        {
            return new LibraryOperationResult(installationState);
        }

        /// <summary>Internal use only</summary>
        public static LibraryOperationResult FromCancelled(ILibraryInstallationState installationState)
        {
            return new LibraryOperationResult(installationState)
            {
                Cancelled = true
            };
        }

        /// <summary>Internal use only</summary>
        public static LibraryOperationResult FromError(IError error)
        {
            return new LibraryOperationResult(error);
        }

        /// <summary>Internal use only</summary>
        public static ILibraryOperationResult FromUpToDate(ILibraryInstallationState installationState)
        {
            return new LibraryOperationResult(installationState)
            {
                UpToDate = true
            };
        }
    }
}
