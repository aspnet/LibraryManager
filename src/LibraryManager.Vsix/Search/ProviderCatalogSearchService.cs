// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
