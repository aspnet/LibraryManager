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
    [Serializable]
    public class InvalidLibraryException : Exception
    {
        /// <summary>
        /// Creates a new instance of the <see cref="InvalidLibraryException"/>.
        /// </summary>
        /// <param name="libraryId">The ID of the invalid library.</param>
        /// <param name="providerId">The ID of the <see cref="IProvider"/> failing to install the library.</param>
        public InvalidLibraryException(string libraryId, string providerId)
            : base(string.Format(Text.ErrorUnableToResolveSource, libraryId, providerId))
        {
            LibraryId = libraryId;
            ProviderId = providerId;
        }

        /// <summary>
        /// Serializable constructor for <see cref="InvalidLibraryException"/>.
        /// </summary>
        protected InvalidLibraryException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
            :this(serializationInfo?.GetString(nameof(LibraryId)), serializationInfo.GetString(nameof(ProviderId)))
        {
        }

        /// <summary>
        /// The ID of the invalid library
        /// </summary>
        public string LibraryId { get; }

        /// <summary>
        /// The ID of the <see cref="IProvider"/> failing to install the library.
        /// </summary>
        public string ProviderId { get; }
    }
}
