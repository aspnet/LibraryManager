// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Cache;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Contracts.Configuration;

namespace Microsoft.Web.LibraryManager.Build.Contracts
{
    internal class HostInteraction : IHostInteraction
    {
        public HostInteraction(string workingDirectory)
        {
            WorkingDirectory = workingDirectory;
        }

        public string WorkingDirectory { get; }
        public string CacheDirectory => CacheService.CacheFolder;
        public ILogger Logger => Contracts.Logger.Instance;
        public ISettings Settings => Configuration.Settings.DefaultSettings;

        public async Task<bool> WriteFileAsync(string path, Func<Stream> content, ILibraryInstallationState state, CancellationToken cancellationToken)
        {
            var absolutePath = new FileInfo(Path.Combine(WorkingDirectory, path));

            if (absolutePath.Exists)
            {
                return true;
            }

            // Note: use ordinal comparison here, as some filesystems are case sensitive.
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

        public Task<bool> DeleteFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken)
        {
            return Task.Run(() => 
            {
                cancellationToken.ThrowIfCancellationRequested();

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
            if (result)
            {
                Logger.Log(string.Format(Resources.Text.FileWrittenToDisk, destinationPath.Replace('\\', '/')), LogLevel.Operation);
            }

            return result;
        }
    }
}
