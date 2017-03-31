// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using LibraryInstaller.Contracts;
using System.Collections.Generic;
using System.Linq;

namespace LibraryInstaller.Mocks
{
    /// <summary>
    /// A mock <see cref="ILibraryInstallationResult"/> class.
    /// </summary>
    /// <seealso cref="LibraryInstaller.Contracts.ILibraryInstallationResult" />
    public class LibraryInstallationResult : ILibraryInstallationResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryInstallationResult"/> class.
        /// </summary>
        public LibraryInstallationResult()
        {
            Errors = new List<IError>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryInstallationResult"/> class.
        /// </summary>
        /// <param name="errors">The errors.</param>
        public LibraryInstallationResult(params IError[] errors)
        {
            var list = new List<IError>();
            list.AddRange(errors);
            Errors = list;
        }

        /// <summary>
        /// <code>True</code> if the installation was cancelled; otherwise false;
        /// </summary>
        public bool Cancelled
        {
            get;
            set;
        }

        /// <summary>
        /// <code>True</code> if the install was successfull; otherwise <code>False</code>.
        /// </summary>
        /// <remarks>
        /// The value is usually <code>True</code> if the <see cref="P:LibraryInstaller.Contracts.ILibraryInstallationResult.Errors" /> list is empty.
        /// </remarks>
        public bool Success
        {
            get { return !Errors.Any(); }
        }

        /// <summary>
        /// A list of errors that occured during library installation.
        /// </summary>
        public IList<IError> Errors
        {
            get;
            set;
        }

        /// <summary>
        /// The <see cref="T:LibraryInstaller.Contracts.ILibraryInstallationState" /> object passed to the
        /// <see cref="T:LibraryInstaller.Contracts.IProvider" /> for installation.
        /// </summary>
        public ILibraryInstallationState InstallationState
        {
            get;
            set;
        }

        /// <summary>
        /// Creates a new <see cref="LibraryInstallationResult"/> that is in a successfull state.
        /// </summary>
        public static LibraryInstallationResult FromSuccess()
        {
            return new LibraryInstallationResult();
        }

        /// <summary>
        /// Creates a new <see cref="LibraryInstallationResult"/> that is in a cancelled state.
        /// </summary>
        public static LibraryInstallationResult FromCancelled()
        {
            return new LibraryInstallationResult
            {
                Cancelled = true
            };
        }
    }
}
