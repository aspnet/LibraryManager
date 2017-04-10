// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Web.LibraryInstaller.Contracts;
using Microsoft.Web.LibraryInstaller.Vsix.Controls.Search;

namespace Microsoft.Web.LibraryInstaller.Vsix.Models
{
    public class PackageSorter : IComparer<ISearchItem>
    {
        private readonly PackageSearchUtil _searchUtil;

        private PackageSorter(string searchTerm)
        {
            _searchUtil = PackageSearchUtil.ForTerm(searchTerm);
        }

        public static IComparer<ISearchItem> For(string searchTerm, IProvider provider)
        {
            return new PackageSorter(searchTerm);
        }

        public int Compare(ISearchItem x, ISearchItem y)
        {
            int leftMatchStrength = _searchUtil.CalculateMatchStrength(x);
            int rightMatchStrength = _searchUtil.CalculateMatchStrength(y);
            int result = -leftMatchStrength.CompareTo(rightMatchStrength);

            if (result == 0)
            {
                result = StringComparer.OrdinalIgnoreCase.Compare(x.CollapsedItemText, y.CollapsedItemText);
            }

            return result;
        }
    }
}