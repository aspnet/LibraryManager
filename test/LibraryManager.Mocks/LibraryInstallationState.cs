// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
