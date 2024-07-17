// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Mocks
{
    /// <summary>
    /// A mock <see cref="IError"/> object.
    /// </summary>
    /// <seealso cref="LibraryManager.Contracts.IError" />
    public class Error : IError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Error"/> class.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="message">The message.</param>
        public Error(string code, string message)
        {
            Code = code;
            Message = message;
        }

        /// <summary>
        /// The error code used to uniquely identify the error.
        /// </summary>
        public virtual string Code { get; set; }

        /// <summary>
        /// The user friendly description of the error.
        /// </summary>
        public virtual string Message { get; set; }
    }
}
