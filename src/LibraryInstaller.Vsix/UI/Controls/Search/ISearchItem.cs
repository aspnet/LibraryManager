// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace LibraryInstaller.Vsix.Controls.Search
{
    public interface ISearchItem
    {
        string CollapsedItemText { get; }

        string Alias { get; }

        bool IsMatchForSearchTerm(string searchTerm);
    }
}
