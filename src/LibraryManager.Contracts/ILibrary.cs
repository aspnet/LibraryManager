// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Web.LibraryManager.Contracts
{
    /// <summary>
    /// Represents a library package
    /// </summary>
    public interface ILibrary
    {
        /// <summary>
        /// The name of the library.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The unique ID of the provider.
        /// </summary>
        string ProviderId { get; }

        /// <summary>
        /// The version of the library.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// A list of files and a <code>bool</code> value to determine if the file is suggested as a default file for this library.
        /// </summary>
        IReadOnlyDictionary<string, bool> Files { get; }
    }
}