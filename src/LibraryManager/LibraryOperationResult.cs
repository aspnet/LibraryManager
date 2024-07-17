// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

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
            Errors = new List<IError>(error);
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
        /// <code>True</code> if the install was successful; otherwise <code>False</code>.
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
        public ILibraryInstallationState? InstallationState { get; set; }

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
