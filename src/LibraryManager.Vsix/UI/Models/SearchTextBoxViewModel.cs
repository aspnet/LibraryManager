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

        /// <summary>
        /// Event to trigger to notify screen readers when the text has been changed.
        /// </summary>
        /// <remarks>
        /// This is for cases where the text value is changed by a user operation other than an edit.
        /// For example, changes due to commiting a completion item, or when influenced by changes in another control.
        /// </remarks>
        public event EventHandler ExternalTextChange;

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

        public virtual Task<CompletionItem> GetRecommendedSelectedCompletionAsync(IEnumerable<CompletionItem> completions, CompletionItem? lastSelected)
        {
            // by default, try to select the same as before, else fall back on the first item in the list
            string lastSelectedText = lastSelected?.InsertionText;
            CompletionItem selectedItem = completions.FirstOrDefault(x => x.InsertionText == lastSelectedText);
            if (selectedItem == default(CompletionItem))
            {
                selectedItem = completions.FirstOrDefault();
            }

            return Task.FromResult(selectedItem);
        }

        /// <summary>
        /// Fire an event to announce to screen readers that the text has been changed.
        /// </summary>
        /// <remarks>
        /// This should be used for non-editing events that modify the value of the text
        /// </remarks>
        public void OnExternalTextChange()
        {
            ExternalTextChange?.Invoke(this, EventArgs.Empty);
        }
    }
}
