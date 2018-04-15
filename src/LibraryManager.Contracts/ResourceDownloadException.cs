// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Web.LibraryManager.Contracts.Resources;

namespace Microsoft.Web.LibraryManager.Contracts
{
    /// <summary>
    /// An exception to be thrown when a library is failing to install because the
    /// information in the <see cref="ILibraryInstallationState"/> is invalid.
    /// </summary>
    /// <remarks>
    /// For instance, if a <see cref="ILibraryInstallationState"/> with an id that isn't
    /// recognized by the <see cref="IProvider"/> is being passed to <see cref="Contracts.IProvider.InstallAsync"/>,
    /// this exception could be thrown so it can be handled inside <see cref="Contracts.IProvider.InstallAsync"/>
    /// and an <see cref="IError"/> added to the <see cref="ILibraryInstallationResult.Errors"/> collection.
    /// </remarks>
    public class ResourceDownloadException : Exception
    {
        /// <summary>
        /// Creates a new instance of the <see cref="InvalidLibraryException"/>.
        /// </summary>
        /// <param name="url">The ID of the invalid library.</param>
        public ResourceDownloadException(string url)
            : base(Text.ErrorUnableToDownloadResource)
        {
            Url = url;
        }

        /// <summary>
        /// The ID of the invalid library
        /// </summary>
        public string Url { get; }

    }
}
