// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryManager.Contracts;
using System.Collections.Generic;

namespace Microsoft.Web.LibraryManager.Mocks
{
    /// <summary>
    /// A mock <see cref="ILibraryInstallationState"/> class.
    /// </summary>
    /// <seealso cref="LibraryManager.Contracts.ILibraryInstallationState" />
    public class LibraryInstallationState : ILibraryInstallationState
    {
        /// <inheritdoc />
        public virtual string ProviderId { get; set; }

        /// <inheritdoc />
        public virtual IReadOnlyList<string> Files { get; set; }

        /// <inheritdoc />
        public virtual string DestinationPath { get; set; }

        /// <inheritdoc />
        public string Name { get; set; }

        /// <inheritdoc />
        public string Version { get; set; }

        /// <inheritdoc />
        public bool IsUsingDefaultDestination { get; set; }

        /// <inheritdoc />
        public bool IsUsingDefaultProvider { get; set; }

        /// <inheritdoc />
        public IReadOnlyList<FileMapping> FileMappings => throw new System.NotImplementedException();
    }
}
