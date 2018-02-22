// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryManager.Contracts;
using System.Collections.Generic;

namespace Microsoft.Web.LibraryManager.Mocks
{
    /// <summary>
    /// A mock <see cref="ILibrary"/> class.
    /// </summary>
    /// <seealso cref="LibraryManager.Contracts.ILibrary" />
    public class Library : ILibrary
    {
        /// <summary>
        /// The string that lets the <see cref="T:LibraryManager.Contracts.IProvider" /> uniquely identify the specific library.
        /// </summary>
        public virtual string Name { get; set; }

        /// <summary>
        /// The unique ID of the provider.
        /// </summary>
        public virtual string ProviderId { get; set; }

        /// <summary>
        /// The version of the library.
        /// </summary>
        public virtual string Version { get; set; }

        /// <summary>
        /// A list of files and a <code>bool</code> value to determine if the file is suggested as a default file for this library.
        /// </summary>
        public virtual IReadOnlyDictionary<string, bool> Files { get; set; }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Name;
        }
    }
}
