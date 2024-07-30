// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix.Search
{
    public class ProviderCatalogSearchService : ISearchService
    {
        private readonly Func<IProvider> _providerLookup;

        public ProviderCatalogSearchService(Func<IProvider> providerLookup)
        {
            _providerLookup = providerLookup ?? throw new ArgumentNullException(nameof(providerLookup));
        }

        public Task<CompletionSet> PerformSearch(string searchText, int caretPosition)
        {
            IProvider provider = _providerLookup();
            if(provider != null)
            {
                return provider.GetCatalog().GetLibraryCompletionSetAsync(searchText, caretPosition);
            }

            return Task.FromResult(default(CompletionSet));
        }
    }
}
