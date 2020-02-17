// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;

namespace Microsoft.Web.LibraryManager.Vsix.Shared
{
    /// <summary>
    /// Contains wrapper methods for Manifest operations calls.
    /// Handles Visual Studio specific behaviors (Task Management, Solution Events listeners and logging)
    /// </summary>
    internal interface ILibraryCommandService
    {
        /// <summary>
        /// Clean the libraries defined in manifest
        /// </summary>
        /// <param name="configProjectItem">ProjectItem for the manifest file (libman.json)</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task CleanAsync(ProjectItem configProjectItem, CancellationToken cancellationToken);

        /// <summary>
        /// Restore libraries from multiple manifest files (libman.json)
        /// </summary>
        /// <param name="configFilePaths">Paths to libman.json files</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <remarks>Used on the Solution Context Menu option to restore all libraries</remarks>
        Task RestoreAsync(IEnumerable<string> configFilePaths, CancellationToken cancellationToken);

        /// <summary>
        /// Restore libraries from a single manifest file (libman.json)
        /// </summary>
        /// <param name="configFilePaths">Paths to libman.json files</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task RestoreAsync(string configFilePaths, CancellationToken cancellationToken);

        /// <summary>
        /// This overload is needed for when Manifest in memory so we don't need to read from file in disk
        /// </summary>
        /// <param name="configFilePath">Path to libman.json</param>
        /// <param name="manifest">In memory Manifest</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task RestoreAsync(string configFilePath, Manifest manifest, CancellationToken cancellationToken);

        /// <summary>
        /// Unsintalls a library from a manifest
        /// </summary>
        /// <param name="configFilePath">libman.json file path</param>
        /// <param name="libraryName">library ID</param>
        /// <param name="version">version of the library</param>
        /// <param name="providerId">Id of the provider for this library</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task UninstallAsync(string configFilePath, string libraryName, string version, string providerId, CancellationToken cancellationToken);

        /// <summary>
        /// Returns true if there is already one operation in progress
        /// </summary>
        bool IsOperationInProgress { get; }

        /// <summary>
        /// Cancels library manager operation in progress
        /// </summary>
        void CancelOperation();
    }
}
