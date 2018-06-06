// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryManager.Contracts;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Web.LibraryManager.Mocks
{
    /// <summary>
    /// A mock <see cref="ILibraryOperationResult"/> class.
    /// </summary>
    /// <seealso cref="LibraryManager.Contracts.ILibraryOperationResult" />
    public class LibraryOperationResult : ILibraryOperationResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryOperationResult"/> class.
        /// </summary>
        public LibraryOperationResult()
        {
            Errors = new List<IError>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryOperationResult"/> class.
        /// </summary>
        /// <param name="errors">The errors.</param>
        public LibraryOperationResult(params IError[] errors)
        {
            var list = new List<IError>();
            list.AddRange(errors);
            Errors = list;
        }

        /// <summary>
        /// <code>True</code> if the installation was cancelled; otherwise false;
        /// </summary>
        public virtual bool Cancelled
        {
            get;
            set;
        }

        /// <summary>
        /// <code>True</code> if the install was successfull; otherwise <code>False</code>.
        /// </summary>
        /// <remarks>
        /// The value is usually <code>True</code> if the <see cref="P:LibraryManager.Contracts.ILibraryOperationResult.Errors" /> list is empty.
        /// </remarks>
        public virtual bool Success
        {
            get { return !Errors.Any() && !Cancelled && !UpToDate; }
        }

        /// <summary>
        /// A list of errors that occured during library installation.
        /// </summary>
        public virtual IList<IError> Errors
        {
            get;
            set;
        }

        /// <summary>
        /// The <see cref="T:LibraryManager.Contracts.ILibraryInstallationState" /> object passed to the
        /// <see cref="T:LibraryManager.Contracts.IProvider" /> for installation.
        /// </summary>
        public virtual ILibraryInstallationState InstallationState
        {
            get;
            set;
        }

        /// <summary>
        /// </summary>
        public virtual bool UpToDate
        {
            get;
            set;
        }

        /// <summary>
        /// Creates a new <see cref="LibraryOperationResult"/> that is in a successfull state.
        /// </summary>
        public static LibraryOperationResult FromSuccess()
        {
            return new LibraryOperationResult();
        }

        /// <summary>
        /// Creates a new <see cref="LibraryOperationResult"/> that is in a cancelled state.
        /// </summary>
        public static LibraryOperationResult FromCancelled()
        {
            return new LibraryOperationResult
            {
                Cancelled = true
            };
        }
    }
}
