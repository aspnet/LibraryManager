// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix.Search
{
    public class LocationSearchService : ISearchService
    {
        private readonly IHostInteraction _hostInteractions;

        public LocationSearchService(IHostInteraction hostInteraction)
        {
            _hostInteractions = hostInteraction ?? throw new ArgumentNullException(nameof(hostInteraction));
        }

        public Task<CompletionSet> PerformSearch(string searchText, int caretPosition)
        {
            searchText = searchText ?? string.Empty;
            if(caretPosition < 0 || caretPosition > searchText.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(caretPosition), $"{nameof(caretPosition)} must be within the bounds of {nameof(searchText)}");
            }

            string searchDir = _hostInteractions.WorkingDirectory;
            string prefix = GetIntermediateFolders(searchText, caretPosition);

            if (prefix.Length > 0)
            {
                searchDir = Path.Combine(searchDir, prefix);
                prefix += "/"; // add the trailing / for the insertionText prefix
            }

            IEnumerable<(string, string)> completions = GetCompletions(searchDir, prefix);

            List<CompletionItem> completionItems = FilterCompletions(searchText, completions);

            var completionSet = new CompletionSet
            {
                Start = 0,
                Length = searchText.Length,
                Completions = completionItems.OrderBy(m => m.InsertionText.IndexOf(searchText, StringComparison.OrdinalIgnoreCase)),
            };

            return Task.FromResult(completionSet);
        }

        private static List<CompletionItem> FilterCompletions(string searchText, IEnumerable<(string, string)> completions)
        {
            var completionItems = new List<CompletionItem>();

            foreach ((string displayText, string insertionText) in completions)
            {
                if (insertionText.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) > -1)
                {
                    var completionItem = new CompletionItem
                    {
                        DisplayText = displayText,
                        InsertionText = insertionText,
                    };

                    completionItems.Add(completionItem);
                }
            }

            return completionItems;
        }

        private IEnumerable<(string displayText, string insertionText)> GetCompletions(string workingDir, string insertionTextPrefix)
        {
            var completions = new List<(string displayText, string insertionText)>();
            
            if (Directory.Exists(workingDir))
            {
                foreach (string item in Directory.EnumerateDirectories(workingDir))
                {
                    string name = Path.GetFileName(item);
                    completions.Add((displayText: name + "/", insertionText: insertionTextPrefix + name + "/"));
                }
            }

            return completions;
        }

        private string GetIntermediateFolders(string value, int caretPosition)
        {
            int index = 0;
            if (value.Contains("/"))
            {
                if (value.Length >= caretPosition - 1)
                {
                    index = value.LastIndexOf('/', Math.Max(caretPosition - 1, 0));

                    if(index < 0)
                    {
                        index = 0;
                    }
                }
                else
                {
                    index = value.Length;
                }
            }

            return value.Substring(0, index);
        }
    }
}
