// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager
{
    /// <summary>
    /// Represents a conflicting file in multiple libraries.
    /// </summary>
    internal class FileConflict
    {
        public FileConflict(string file, List<ILibraryInstallationState> libraries)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentException($"{nameof(file)} cannot be null or empty.", nameof(file));
            }

            File = file;
            Libraries = libraries ?? throw new ArgumentNullException(nameof(libraries));
        }

        public string File { get; }
        public IList<ILibraryInstallationState> Libraries { get; }
    }
}
