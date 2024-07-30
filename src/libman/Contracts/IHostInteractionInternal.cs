// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
