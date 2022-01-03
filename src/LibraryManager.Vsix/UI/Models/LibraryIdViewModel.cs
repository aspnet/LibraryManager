// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Vsix.Search;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Models
{
    internal class LibraryIdViewModel : SearchTextBoxViewModel
    {
        public LibraryIdViewModel(ISearchService packageSearchService, string initialSearchText)
            : base(packageSearchService,
                   initialSearchText,
                   watermarkText: Resources.Text.TypeToSearch,
                   automationName: Resources.Text.Library)
        {
        }

        protected override Task<CompletionSet> FilterCompletions(CompletionSet input)
        {
            CompletionSet result = input;
            int atIndex = SearchText.IndexOf('@', 1);

            if (atIndex >= 0)
            {
                string versionSuffix = SearchText.Substring(atIndex + 1);
                IEnumerable<CompletionItem> filteredCompletions = input.Completions.Where(x => x.DisplayText.Contains(versionSuffix));

                result = new CompletionSet
                {
                    Start = input.Start,
                    Length = input.Length,
                    CompletionType = input.CompletionType,
                    Completions = filteredCompletions,
                };
            }

            return Task.FromResult(result);
        }

        public override async Task<CompletionItem> GetRecommendedSelectedCompletionAsync(IEnumerable<CompletionItem> completions, CompletionItem? lastSelected)
        {
            int atIndex = SearchText.IndexOf('@');
            var result = default(CompletionItem);

            if (atIndex >= 0)
            {
                // if we're in the version portion, try to select the first item that starts with the version
                string versionPortion = SearchText.Substring(atIndex + 1);
                Func<CompletionItem, bool> predicate = x => x.DisplayText.StartsWith(versionPortion, StringComparison.OrdinalIgnoreCase);
                result = completions.FirstOrDefault(predicate);
                if (result == default(CompletionItem))
                {
                    result = completions.FirstOrDefault();
                }
            }
            else
            {
                result = await base.GetRecommendedSelectedCompletionAsync(completions, lastSelected);
            }

            return result;
        }
    }
}
