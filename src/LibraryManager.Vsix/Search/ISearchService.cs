// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix.Search
{
    public interface ISearchService
    {
        Task<CompletionSet> PerformSearch(string searchText, int caretPosition);
    }
}
