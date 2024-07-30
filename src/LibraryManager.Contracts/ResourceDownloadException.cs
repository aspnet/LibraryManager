// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
