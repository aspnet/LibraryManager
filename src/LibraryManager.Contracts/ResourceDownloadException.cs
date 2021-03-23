// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Web.LibraryManager.Contracts.Resources;

namespace Microsoft.Web.LibraryManager.Contracts
{
    /// <summary>
    /// An exception to be thrown when a fails to be downloaded
    /// </summary>
    /// <remarks>
    /// </remarks>
    [Serializable]
    public class ResourceDownloadException : Exception
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ResourceDownloadException"/>.
        /// </summary>
        /// <param name="url">The url for the resource.</param>
        public ResourceDownloadException(string url)
            : this(string.Format(Text.ErrorUnableToDownloadResource, url), null)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ResourceDownloadException"/>.
        /// </summary>
        /// <param name="url">The url for the resource.</param>
        /// <param name="innerException">Inner exception</param>
        public ResourceDownloadException(string url, Exception innerException)
            : base(string.Format(Text.ErrorUnableToDownloadResource, url), innerException)
        {
            Url = url;
        }

        /// <summary>
        /// The Url for the of the invalid library
        /// </summary>
        public string Url { get; }

        /// <summary>
        /// Serialization constructor for this exception type.
        /// </summary>
        protected ResourceDownloadException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
            : this(serializationInfo?.GetString(nameof(Url)))
        {
        }
    }
}
