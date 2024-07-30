// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Web.LibraryManager.Contracts
{
    /// <summary>
    /// Represents the result of <see cref="IProvider.InstallAsync"/>.
    /// </summary>
    public interface ILibraryOperationResult
    {
        /// <summary>
        /// <code>True</code> if the installation was cancelled; otherwise false;
        /// </summary>
        bool Cancelled { get; }

        /// <summary>
        /// <code>True</code> if the install was successfull; otherwise <code>False</code>.
        /// </summary>
        /// <remarks>
        /// The value is usually <code>True</code> if the <see cref="Errors"/> list is empty.
        /// </remarks>
        bool Success { get; }

        /// <summary>
        /// <code>True</code> if the library is up to date; otherwise <code>False</code>.
        /// </summary>
        /// <remarks>
        /// </remarks>
        bool UpToDate { get; }

        /// <summary>
        /// A list of errors that occured during library installation.
        /// </summary>
        IList<IError> Errors { get; }

        /// <summary>
        /// The <see cref="ILibraryInstallationState"/> object passed to the
        /// <see cref="IProvider"/> for installation.
        /// </summary>
        ILibraryInstallationState InstallationState { get; }
    }
}
