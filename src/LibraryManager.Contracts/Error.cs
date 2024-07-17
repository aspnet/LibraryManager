﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Web.LibraryManager.Contracts
{
    /// <summary>
    /// A basic implementation of <see cref="IError"/>.
    /// </summary>
    /// <seealso cref="Microsoft.Web.LibraryManager.Contracts.IError" />
    internal class Error : IError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Error"/> class.
        /// </summary>
        /// <param name="code">The error code.</param>
        /// <param name="message">A detailed error message.</param>
        public Error(string code, string message)
        {
            Code = code;
            Message = message;
        }

        /// <summary>
        /// The error code used to uniquely identify the error.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// The user friendly description of the error.
        /// </summary>
        public string Message { get; set; }
    }
}
