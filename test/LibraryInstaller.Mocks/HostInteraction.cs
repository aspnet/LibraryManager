// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryManager.Contracts;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace Microsoft.Web.LibraryManager.Mocks
{
    /// <summary>
    /// A class provided by the host to handle file writes etc.
    /// </summary>
    public class HostInteraction : IHostInteraction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HostInteraction"/> class.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="cacheFolder">The cache folder.</param>
        public HostInteraction(string workingDirectory, string cacheFolder)
        {
            WorkingDirectory = workingDirectory;
            CacheDirectory = cacheFolder;
        }

        /// <summary>
        /// The directory on disk the <see cref="IProvider"/> should use for caching purposes if caching is needed.
        /// </summary>
        /// <remarks>
        /// The cache directory is not being created, so each <see cref="IProvider"/> should ensure to do that if needed.
        /// </remarks>
        public virtual string CacheDirectory { get; }

        /// <summary>
        /// The root directory from where file paths are calculated.
        /// </summary>
        public virtual string WorkingDirectory { get; }

        /// <summary>
        /// Gets the logger associated with the host.
        /// </summary>
        public virtual ILogger Logger { get; } = new Logger();

        /// <summary>
        /// Writes a file to disk based on the specified <see cref="ILibraryInstallationState"/>.
        /// </summary>
        /// <param name="path">The full file path</param>
        /// <param name="content">The content of the file to write.</param>
        /// <param name="state">The desired state of the finished installed library.</param>
        /// <param name="cancellationToken">A token that allows cancellation of the file writing.</param>
        /// <returns><code>True</code> if no issues occured while executing this method; otherwise <code>False</code>.</returns>
        public virtual async Task<bool> WriteFileAsync(string path, Func<Stream> content, ILibraryInstallationState state, CancellationToken cancellationToken)
        {
            var absolutePath = new FileInfo(Path.Combine(WorkingDirectory, path));

            if (absolutePath.Exists)
                return true;

            if (!absolutePath.FullName.StartsWith(WorkingDirectory))
                throw new UnauthorizedAccessException();

            absolutePath.Directory.Create();

            using (Stream stream = content.Invoke())
            {
                if (stream == null)
                    return false;

                using (FileStream writer = File.Create(absolutePath.FullName, 8192, FileOptions.Asynchronous))
                {
                    if (stream.CanSeek)
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                    }

                    await stream.CopyToAsync(writer, 8192, cancellationToken).ConfigureAwait(false);
                }
            }

            return true;
        }

        /// <summary>
        /// Deletes a file from disk.
        /// </summary>
        /// <param name="relativeFilePath">The absolute path to the file.</param>
        public virtual void DeleteFile(string relativeFilePath)
        {
            string absoluteFile = Path.Combine(WorkingDirectory, relativeFilePath);
            File.Delete(absoluteFile);
        }
    }
}
