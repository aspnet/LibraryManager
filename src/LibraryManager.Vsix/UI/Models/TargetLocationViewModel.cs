﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using Microsoft.Web.LibraryManager.Vsix.Search;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Models
{
    internal class TargetLocationViewModel : SearchTextBoxViewModel
    {
        private readonly string _baseFolder;
        private readonly LibraryNameBinding _libraryNameBinding;
        private string _lastSuggestedTargetLocation;

        public TargetLocationViewModel(string baseFolder,
                                       LibraryNameBinding libraryNameBinding,
                                       ISearchService searchService)
            : base(searchService, baseFolder, null, automationName: Resources.Text.TargetLocation)
        {
            _baseFolder = baseFolder ?? string.Empty;
            _lastSuggestedTargetLocation = baseFolder ?? string.Empty;
            SearchText = baseFolder;

            _libraryNameBinding = libraryNameBinding ?? throw new ArgumentNullException(nameof(libraryNameBinding));
            _libraryNameBinding.PropertyChanged += LibraryNameChanged;
        }

        private void LibraryNameChanged(object sender, PropertyChangedEventArgs e)
        {
            string targetLibrary = _libraryNameBinding.LibraryName;

            if (!string.IsNullOrEmpty(targetLibrary))
            {
                if (SearchText.Equals(_lastSuggestedTargetLocation, StringComparison.OrdinalIgnoreCase))
                {
                    // remove any trailing forward slashes, because we probably put them there
                    targetLibrary = targetLibrary.TrimEnd('/');

                    SearchText = _lastSuggestedTargetLocation = _baseFolder + targetLibrary + '/';
                    OnExternalTextChange();
                }
            }
        }
    }
}
