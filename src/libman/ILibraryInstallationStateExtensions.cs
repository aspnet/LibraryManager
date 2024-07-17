﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.LibraryNaming;

namespace Microsoft.Web.LibraryManager.Tools
{
    /// <summary>
    /// Extension Methods for <see cref="ILibraryInstallationState"/>
    /// </summary>
    internal static class ILibraryInstallationStateExtensions
    {
        /// <summary>
        /// Returns a string that can be used to display the library information on the console.
        /// </summary>
        /// <param name="libraryInstallationState"></param>
        /// <returns></returns>
        public static string ToConsoleDisplayString(this ILibraryInstallationState libraryInstallationState)
        {
            if (libraryInstallationState == null)
            {
                throw new ArgumentNullException(nameof(libraryInstallationState));
            }

            string libraryId = LibraryIdToNameAndVersionConverter.Instance.GetLibraryId(
                                    libraryInstallationState.Name,
                                    libraryInstallationState.Version,
                                    libraryInstallationState.ProviderId);

            var sb = new StringBuilder("{"+libraryId);

            if (!string.IsNullOrEmpty(libraryInstallationState.ProviderId))
            {
                sb.Append(", " + libraryInstallationState.ProviderId);
            }

            if (!string.IsNullOrEmpty(libraryInstallationState.DestinationPath))
            {
                sb.Append(", " + libraryInstallationState.DestinationPath);
            }

            sb.Append('}');

            return sb.ToString();
        }
    }
}
