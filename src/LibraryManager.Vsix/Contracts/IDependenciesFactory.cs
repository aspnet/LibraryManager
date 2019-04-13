// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix.Contracts
{
    internal interface IDependenciesFactory
    {
        IDependencies FromConfigFile(string configFilePath);
    }
}
