// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Contracts.Configuration;

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

            Logger = settings.Logger ?? throw new ArgumentException($"{nameof(settings)} must have a non-null {nameof(settings.Logger)}", nameof(settings));

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
        public ISettings Settings => Configuration.Settings.DefaultSettings;

        /// <inheritdoc />
        public async Task<bool> WriteFileAsync(string path, Func<Stream> content, ILibraryInstallationState state, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var absolutePath = new FileInfo(Path.Combine(WorkingDirectory, path));

            if (absolutePath.Exists)
            {
                return true;
            }

            // Note: using ordinal comparison as some filesystems are case sensitive.
            if (!absolutePath.FullName.StartsWith(WorkingDirectory, StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException();
            }

            cancellationToken.ThrowIfCancellationRequested();
            absolutePath.Directory.Create();

            using (Stream stream = content.Invoke())
            {
                if (stream == null || !await FileHelpers.SafeWriteToFileAsync(absolutePath.FullName, stream, cancellationToken).ConfigureAwait(false))
                {
                    return false;
                }
            }

            Logger.Log(string.Format(Resources.Text.FileWrittenToDisk, path.Replace('\\', '/')), LogLevel.Operation);

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
                return FileHelpers.DeleteFiles(filePaths, WorkingDirectory);
            }, cancellationToken);
        }

        public Task<Stream> ReadFileAsync(string relativeFilePath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return FileHelpers.ReadFileAsStreamAsync(relativeFilePath, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> CopyFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string absoluteDestinationPath = Path.Combine(WorkingDirectory, destinationPath);
            if (!FileHelpers.IsUnderRootDirectory(absoluteDestinationPath, WorkingDirectory))
            {
                throw new UnauthorizedAccessException();
            }

            bool result = await FileHelpers.CopyFileAsync(sourcePath, absoluteDestinationPath, cancellationToken);
            if(result)
            {
                Logger.Log(string.Format(Resources.Text.FileWrittenToDisk, destinationPath.Replace('\\', '/')), LogLevel.Operation);
            }

            return result;
        }
    }
}
