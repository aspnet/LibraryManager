// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
