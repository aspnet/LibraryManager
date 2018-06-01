// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Tools.Contracts
{
    /// <summary>
    /// Provides a way to interact with the host environment.
    /// </summary>
    interface IHostInteractionInternal : IHostInteraction
    {
        /// <summary>
        /// Allows updating the working directory.
        /// </summary>
        /// <param name="directory"></param>
        void UpdateWorkingDirectory(string directory);
    }
}
