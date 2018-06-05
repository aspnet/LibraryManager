// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager
{
    /// <summary>
    /// A collection of extension methods.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Returns true if the <see cref="ILibraryInstallationState"/> is valid.
        /// </summary>
        /// <param name="state">The state to test.</param>
        /// <param name="errors">The errors contained in the <see cref="ILibraryInstallationState"/> if any.</param>
        /// <returns>
        ///   <c>true</c> if the specified state is valid; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsValid(this ILibraryInstallationState state, out IEnumerable<IError> errors)
        {
            errors = null;
            var list = new List<IError>();

            if (state == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(state.ProviderId))
            {
                list.Add(PredefinedErrors.ProviderIsUndefined());
            }

            if (string.IsNullOrEmpty(state.DestinationPath))
            {
                list.Add(PredefinedErrors.PathIsUndefined());
            }
            else if (state.DestinationPath.IndexOfAny(Path.GetInvalidPathChars()) > 0)
            {
                list.Add(PredefinedErrors.DestinationPathHasInvalidCharacters(state.DestinationPath));
            }

            if (string.IsNullOrEmpty(state.LibraryId))
            {
                list.Add(PredefinedErrors.LibraryIdIsUndefined());
            }

            errors = list;

            return list.Count == 0;
        }

        /// <summary>
        /// Returns files from <paramref name="files"/> that are not part of the <paramref name="library"/>
        /// </summary>
        /// <param name="library"></param>
        /// <param name="files"></param>
        /// <returns></returns>
        public static IReadOnlyList<string> GetInvalidFiles(this ILibrary library, IReadOnlyList<string> files)
        {
            if (library == null)
            {
                throw new ArgumentNullException(nameof(library));
            }

            var invalidFiles = new List<string>();

            if (files == null || !files.Any())
            {
                return invalidFiles;
            }

            foreach(string file in files)
            {
                if (!library.Files.ContainsKey(file))
                {
                    invalidFiles.Add(file);
                }
            }

            return invalidFiles;
        }
    }
}
