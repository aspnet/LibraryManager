// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Providers.Shared
{
    internal static class ProvidersCommon
    {
        private const char _idPartsSeparator = '@';

        public static LibraryIdentifier GetLibraryIdentifier(IProvider provider, string libraryId)
        {
            // A valid libraryId:
            // - can not be null or empty string
            // - has at least one _idPartsSeparator
            // - can not end with a _idPartsSeparator
            // - can start with a _idPartsSeparator
            // - can not start or end with space
            // - each part (Name, Version) can not start or end with space 

            if (string.IsNullOrEmpty(libraryId) ||
                !libraryId.Contains(_idPartsSeparator.ToString()) ||
                libraryId.EndsWith(_idPartsSeparator.ToString()) ||
                char.IsWhiteSpace(libraryId[0]) ||
                char.IsWhiteSpace(libraryId[libraryId.Length - 1]))
            {
                throw new InvalidLibraryException(libraryId, provider.Id, Resources.Text.NameAndVersionRequired);
            }

            int separatorIndex = libraryId.LastIndexOf(_idPartsSeparator);
            string[] parts = { libraryId.Substring(0, separatorIndex), libraryId.Substring(separatorIndex + 1) };

            foreach (string part in parts)
            {
                if (string.IsNullOrEmpty(part) ||
                    char.IsWhiteSpace(part[0]) ||
                    char.IsWhiteSpace(part[part.Length - 1]))
                {
                    throw new InvalidLibraryException(libraryId, provider.Id, Resources.Text.NameAndVersionRequired);
                }
            }

            return new LibraryIdentifier(parts[0], parts[1]);
        }
    }
}
