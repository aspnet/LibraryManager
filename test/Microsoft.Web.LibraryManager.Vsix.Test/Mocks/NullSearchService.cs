// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Vsix.Search;

namespace Microsoft.Web.LibraryManager.Vsix.Test.Mocks
{
    internal class NullSearchService : ISearchService
    {
        public Task<CompletionSet> PerformSearch(string searchText, int caretPosition)
        {
            return Task.FromResult(default(CompletionSet));
        }
    }
}
