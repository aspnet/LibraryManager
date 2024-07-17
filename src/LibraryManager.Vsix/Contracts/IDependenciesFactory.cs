// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix.Contracts
{
    internal interface IDependenciesFactory
    {
        IDependencies FromConfigFile(string configFilePath);
    }
}
