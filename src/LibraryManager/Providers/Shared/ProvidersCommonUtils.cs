// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Providers.Shared
{
    internal static class ProvidersCommonUtils
    {
        private const char _idPartsSeparator = '@';

        public static ILibrary GetLibraryIdentifier(string providerId, string libraryId)
        {
            // A valid libraryId:
            // - can not be null or empty string
            // - has at least one _idPartsSeparator
            // - can not end with a _idPartsSeparator
            // - can start with a _idPartsSeparator
            // - can not start or end with space
            // - must have two parts (Name and Version)
            // - each part (Name, Version) can not start or end with space 

            if (string.IsNullOrEmpty(libraryId) ||
                libraryId.IndexOf(_idPartsSeparator) < 0 ||
                libraryId[libraryId.Length - 1] == _idPartsSeparator ||
                char.IsWhiteSpace(libraryId[0]) ||
                char.IsWhiteSpace(libraryId[libraryId.Length - 1]))
            {
                throw new InvalidLibraryException(libraryId, providerId , Resources.Text.NameAndVersionRequired);
            }

            int separatorIndex = libraryId.LastIndexOf(_idPartsSeparator);
            string[] parts = { libraryId.Substring(0, separatorIndex), libraryId.Substring(separatorIndex + 1) };

            foreach (string part in parts)
            {
                if (string.IsNullOrEmpty(part) ||
                    char.IsWhiteSpace(part[0]) ||
                    char.IsWhiteSpace(part[part.Length - 1]))
                {
                    throw new InvalidLibraryException(libraryId, providerId, Resources.Text.NameAndVersionRequired);
                }
            }

            return new Library { Name = parts[0], Version = parts[1], ProviderId = providerId };
        }
    }
}
