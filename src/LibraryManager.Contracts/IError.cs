// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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