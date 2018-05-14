// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Tools
{
    internal static class ILibraryInstallationStateExtensions
    {
        public static string ToConsoleDisplayString(this ILibraryInstallationState libraryInstallationState)
        {
            if (libraryInstallationState == null)
            {
                throw new ArgumentNullException(nameof(libraryInstallationState));
            }

            var sb = new StringBuilder("{ "+libraryInstallationState.LibraryId);

            if (!string.IsNullOrEmpty(libraryInstallationState.ProviderId))
            {
                sb.Append(", " + libraryInstallationState.ProviderId);
            }

            if (!string.IsNullOrEmpty(libraryInstallationState.DestinationPath))
            {
                sb.Append(", " + libraryInstallationState.DestinationPath);
            }

            sb.Append(" }");

            return sb.ToString();
        }
    }
}
