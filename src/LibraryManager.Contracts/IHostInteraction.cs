// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts.Configuration;

namespace Microsoft.Web.LibraryManager.Contracts
{
    /// <summary>
    /// A class provided by the host to handle file writes etc.
    /// </summary>
    public interface IHostInteraction
    {
        /// <summary>
        /// The directory on disk the <see cref="IProvider"/> should use for caching purposes if caching is needed.
        /// </summary>
        /// <remarks>
        /// The cache directory is not being created, so each <see cref="IProvider"/> should ensure to do that if needed.
        /// </remarks>
        string CacheDirectory { get; }

        /// <summary>
        /// The root directory from where file paths are calculated.
        /// </summary>
        string WorkingDirectory { get; }

        /// <summary>
        /// Gets the logger associated with the host.
        /// </summary>
        ILogger Logger { get; }

        /// <summary>
        /// Gets access to the configuration settings
        /// </summary>
        ISettings Settings { get; }

        /// <summary>
        /// Writes a file to disk based on the specified <see cref="ILibraryInstallationState"/>.
        /// </summary>
        /// <param name="filePath">The relative file path to the WorkingDirectory </param>
        /// <param name="content">The content of the file to write.</param>
        /// <param name="state">The desired state of the finished installed library.</param>
        /// <param name="cancellationToken">A token that allows cancellation of the file writing.</param>
        /// <returns><code>True</code> if no issues occured while executing this method; otherwise <code>False</code>.</returns>
        Task<bool> WriteFileAsync(string filePath, Func<Stream> content, ILibraryInstallationState state, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes files based on the given collection of relative paths to the WorkingDirectory
        /// </summary>
        /// <param name="filePaths"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> DeleteFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken);

        /// <summary>
        /// Reads a file as a Stream 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Stream> ReadFileAsync(string filePath, CancellationToken cancellationToken);

        /// <summary>
        /// Copies a file
        /// </summary>
        /// <param name="sourcePath">The absolute path to the source file</param>
        /// <param name="destinationPath">The relative path to the destination file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A boolean indicating if the copy was successful</returns>
        Task<bool> CopyFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken);
    }
}
