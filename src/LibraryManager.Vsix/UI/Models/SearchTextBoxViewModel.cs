// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Vsix.Search;
using Microsoft.Web.LibraryManager.Vsix.UI.Controls;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Models
{
    internal abstract class SearchTextBoxViewModel : BindableBase
    {
        private string _searchText;

        public SearchTextBoxViewModel(ISearchService searchService,
                                      string initialSearchText,
                                      string watermarkText,
                                      string automationName)
        {
            SearchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
            automationName = CleanupAutomationName(automationName);
            if(string.IsNullOrEmpty(automationName))
            {
                throw new ArgumentException($"{nameof(automationName)} cannot be empty");
            }

            AutomationName = automationName;
            SearchText = initialSearchText ?? string.Empty;
            WatermarkText = watermarkText ?? string.Empty;
        }

        public ISearchService SearchService { get; private set; }
        public string SearchText
        {
            get => _searchText;
            set => Set(ref _searchText, value);
        }

        public string AutomationName { get; }
        public string WatermarkText { get; }

        private static string CleanupAutomationName(string automationName)
        {
            string cleanedName = automationName?.Replace("_", "");

            return cleanedName;
        }

        public async Task<CompletionSet> GetCompletionSetAsync(int caretIndex)
        {
            CompletionSet completionSet = await SearchService?.PerformSearch(SearchText, caretIndex);

            completionSet = await FilterCompletions(completionSet);

            return completionSet;
        }

        protected virtual Task<CompletionSet> FilterCompletions(CompletionSet input)
        {
            return Task.FromResult(input);
        }

        public virtual Task<CompletionItem> GetRecommendedSelectedCompletionAsync(CompletionSet completionSet, CompletionItem? lastSelected)
        {
            // by default, try to select the same as before, else fall back on the first item in the list
            string lastSelectedText = lastSelected?.InsertionText;
            CompletionItem selectedItem = completionSet.Completions.FirstOrDefault(x => x.InsertionText == lastSelectedText);
            if (selectedItem == default(CompletionItem))
            {
                selectedItem = completionSet.Completions.FirstOrDefault();
            }

            return Task.FromResult(selectedItem);
        }
    }
}
