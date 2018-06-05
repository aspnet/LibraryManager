// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;

namespace Microsoft.Web.LibraryManager.Vsix
{
    /// <summary>
    /// Contains wrapper methods for Manifest operations calls. 
    /// Handles Visual Studio specific behaviors (Task Management, Solution Events listeners and logging)
    /// </summary>
    internal interface ILibraryCommandService
    {
        /// <summary>
        /// Clean the libraries on a manifest associated with a ProjectItem
        /// </summary>
        /// <param name="configProjectItem"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task CleanAsync(ProjectItem configProjectItem, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Restore libraries from multiple manifests
        /// </summary>
        /// <param name="configFilePaths">manifest files paths</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task RestoreAsync(IEnumerable<string> configFilePaths, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Unsintall a library from a manifest 
        /// </summary>
        /// <param name="configFilePath">libman.json file path</param>
        /// <param name="libraryId">library ID</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task UninstallAsync(string configFilePath, string libraryId, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns true if there is already one operation in progress
        /// </summary>
        bool IsOperationInProgress { get; }

        /// <summary>
        /// Cancels operation in progress
        /// </summary>
        void CancelOperation();
    }
}
