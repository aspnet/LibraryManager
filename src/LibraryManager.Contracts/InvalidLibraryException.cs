// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
    /// and an <see cref="IError"/> added to the <see cref="ILibraryOperationResult.Errors"/> collection.
    /// </remarks>
    public class InvalidLibraryException : Exception
    {
        /// <summary>
        /// Creates a new instance of the <see cref="InvalidLibraryException"/>.
        /// </summary>
        /// <param name="libraryId">The ID of the invalid library.</param>
        /// <param name="providerId">The ID of the <see cref="IProvider"/> failing to install the library.</param>
        /// <param name="details">Additional information about the exception</param>
        public InvalidLibraryException(string libraryId, string providerId, string details = null)
            : base(GenerateMessage(libraryId, providerId, details))
        {
            LibraryId = libraryId;
            ProviderId = providerId;
            Details = details;
        }

        private static string GenerateMessage(string libraryId, string providerId, string details = null)
        {
            string message = string.Format(Text.ErrorUnableToResolveSource, libraryId ?? string.Empty, providerId ?? string.Empty);

            if(details != null && !string.IsNullOrWhiteSpace(details))
            {
                message += Environment.NewLine + details;
            }

            return message;
        }

        /// <summary>
        /// The ID of the invalid library
        /// </summary>
        public string LibraryId { get; }

        /// <summary>
        /// The ID of the <see cref="IProvider"/> failing to install the library.
        /// </summary>
        public string ProviderId { get; }

        /// <summary>
        /// Details of the exception.
        /// </summary>
        public string Details { get; }
    }
}
