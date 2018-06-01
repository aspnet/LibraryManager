// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Tools.Contracts
{
    /// <inheritdoc />
    internal class HostInteraction : IHostInteractionInternal
    {
        public HostInteraction(EnvironmentSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            Logger = settings.Logger ?? throw new ArgumentNullException(nameof(settings.Logger));

            WorkingDirectory = settings.CurrentWorkingDirectory;
            CacheDirectory = settings.CacheDirectory;
        }

        /// <inheritdoc />
        public string WorkingDirectory { get; private set; }

        /// <inheritdoc />
        public string CacheDirectory { get; }

        /// <inheritdoc />
        public ILogger Logger { get; }

        /// <inheritdoc />
        public async Task<bool> WriteFileAsync(string path, Func<Stream> content, ILibraryInstallationState state, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var absolutePath = new FileInfo(Path.Combine(WorkingDirectory, path));

            if (absolutePath.Exists)
            {
                return true;
            }

            if (!absolutePath.FullName.StartsWith(WorkingDirectory))
            {
                throw new UnauthorizedAccessException();
            }

            cancellationToken.ThrowIfCancellationRequested();
            absolutePath.Directory.Create();

            using (Stream stream = content.Invoke())
            {
                if (stream == null || !await FileHelpers.WriteToFileAsync(absolutePath.FullName, stream, cancellationToken).ConfigureAwait(false))
                {
                    return false;
                }
            }

            Logger.Log(string.Format(Resources.FileWrittenToDisk, path.Replace('\\', '/')), LogLevel.Operation);

            return true;
        }

        /// <inheritdoc />
        public void UpdateWorkingDirectory(string directory)
        {
            WorkingDirectory = directory;
        }

        public Task<bool> DeleteFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                filePaths = filePaths.Select(f => Path.Combine(WorkingDirectory, f));
                return FileHelpers.DeleteFiles(filePaths);
            }, cancellationToken);
        }

        public Task<Stream> ReadFileAsync(string relativeFilePath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return FileHelpers.ReadFileAsStreamAsync(relativeFilePath, cancellationToken);
        }

        public async Task<bool> CopyFile(string sourcePath, string destinationPath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return await Task.Run(() => { return FileHelpers.CopyFile(sourcePath, destinationPath); });
        }
    }
}
