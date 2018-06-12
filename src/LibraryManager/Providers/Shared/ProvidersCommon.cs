// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Providers.Shared
{
    internal static class ProvidersCommon
    {
        internal const string VersionIdPart = "Version";
        internal const string NameIdPart = "Name";
        private const char _idPartsSeparator = '@';

        public static IDictionary<string, string> GetLibraryIdParts(IProvider provider, string libraryId)
        {
            Dictionary<string, string> libraryIdParts = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(libraryId) ||
                !libraryId.Contains(_idPartsSeparator.ToString()) ||
                libraryId.StartsWith(_idPartsSeparator.ToString()) ||
                libraryId.EndsWith(_idPartsSeparator.ToString()))
            {
                throw new InvalidLibraryException(libraryId, provider.Id);
            }

            string[] args = libraryId.Split(_idPartsSeparator);

            foreach (string arg in args)
            {
                if (string.IsNullOrEmpty(arg) ||
                   arg.StartsWith(string.Empty) ||
                   arg.EndsWith(string.Empty))
                {
                    throw new InvalidLibraryException(libraryId, provider.Id);
                }
            }

            libraryIdParts.Add("_nameIdPart", args[0]);
            libraryIdParts.Add("_versionIdPart", args[1]);

            return libraryIdParts;
        }
    }
}
