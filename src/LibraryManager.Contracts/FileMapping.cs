// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.Web.LibraryManager.Contracts
{
    /// <summary>
    /// 
    /// </summary>
    public class FileMapping
    {
        /// <summary>
        /// Root path within the library content for this file mapping entry.
        /// </summary>
        public string? Root { get; set; }

        /// <summary>
        /// Destination folder within the project.
        /// </summary>
        public string? Destination { get; set; }

        /// <summary>
        /// The file patterns to match for this mapping, relative to <see cref="Root"/>.  Accepts glob patterns.
        /// </summary>
        public IReadOnlyList<string>? Files { get; set; }
    }
}
