// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
