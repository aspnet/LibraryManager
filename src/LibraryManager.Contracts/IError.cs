// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Web.LibraryManager.Contracts
{
    /// <summary>
    /// A object returned from <see cref="IProvider.InstallAsync"/> method in case of any errors occured during installation.
    /// </summary>
    public interface IError
    {
        /// <summary>
        /// The error code used to uniquely identify the error.
        /// </summary>
        string Code { get; }

        /// <summary>
        /// The user friendly description of the error.
        /// </summary>
        string Message { get; }
    }
}