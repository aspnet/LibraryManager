// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Build
{
    internal class HostInteraction : IHostInteraction
    {
        public HostInteraction(string workingDirectory)
        {
            WorkingDirectory = workingDirectory;
        }

        public string WorkingDirectory { get; }
        public string CacheDirectory => Constants.CacheFolder;
        public ILogger Logger => Build.Logger.Instance;

        public async Task<bool> WriteFileAsync(string path, Func<Stream> content, ILibraryInstallationState state, CancellationToken cancellationToken)
        {
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

        public Task<bool> CopyFile(string sourcePath, string destinationPath, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                return FileHelpers.CopyFile(sourcePath, destinationPath);
            }, cancellationToken);
        }
    }
}
