// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using LibraryInstaller.Contracts;
using System.Collections.Generic;

namespace LibraryInstaller.Mocks
{
    /// <summary>
    /// A mock <see cref="ILibrary"/> class.
    /// </summary>
    /// <seealso cref="LibraryInstaller.Contracts.ILibrary" />
    public class Library : ILibrary
    {
        /// <summary>
        /// The string that lets the <see cref="T:LibraryInstaller.Contracts.IProvider" /> uniquely identify the specific library.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The unique ID of the provider.
        /// </summary>
        public string ProviderId { get; set; }

        /// <summary>
        /// The version of the library.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// A list of files and a <code>bool</code> value to determine if the file is suggested as a default file for this library.
        /// </summary>
        public IReadOnlyDictionary<string, bool> Files { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Id;
        }
    }
}
