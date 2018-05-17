// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
            var absolutePath = new FileInfo(Path.Combine(WorkingDirectory, path));

            if (absolutePath.Exists)
            {
                return true;
            }

            if (!absolutePath.FullName.StartsWith(WorkingDirectory))
            {
                throw new UnauthorizedAccessException();
            }

            absolutePath.Directory.Create();

            using (Stream stream = content.Invoke())
            {
                if (stream == null)
                {
                    return false;
                }

                using (FileStream writer = File.Create(absolutePath.FullName, 4096, FileOptions.Asynchronous))
                {
                    if (stream.CanSeek)
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                    }

                    await stream.CopyToAsync(writer, 8192, cancellationToken).ConfigureAwait(false);
                }
            }

            Logger.Log(string.Format(Resources.FileWrittenToDisk, path.Replace('\\', '/')), LogLevel.Operation);

            return true;
        }

        /// <inheritdoc />
        public void DeleteFiles(params string[] relativeFilePaths)
        {
            foreach (string relativeFilePath in relativeFilePaths)
            {
                string absoluteFile = Path.Combine(WorkingDirectory, relativeFilePath);

                try
                {
                    string directoryName = Path.GetDirectoryName(absoluteFile);
                    File.Delete(absoluteFile);
                    DeleteEmptyDirectories(directoryName);

                    Logger.Log(string.Format(Resources.FileDeleted, relativeFilePath), LogLevel.Operation);
                }
                catch (Exception)
                {
                    Logger.Log(string.Format(Resources.FileDeleteFail, relativeFilePath), LogLevel.Operation);
                }
            }
        }

        private void DeleteEmptyDirectories(string directoryName)
        {
            // Since we did a path.Combine(WorkingDirectory, "...") to arrive at directoryName,
            // We keep deleting empty directories till we reach back to the workingDirectory.
            // The working directory will also not be empty because libman.json would in the directory.
            while (WorkingDirectory.TrimEnd('\\', '/') != directoryName.TrimEnd('\\', '/')
                    && !Directory.EnumerateFiles(directoryName).Any()
                    && !Directory.EnumerateDirectories(directoryName).Any())
            {
                Directory.Delete(directoryName);
                directoryName = Path.GetDirectoryName(directoryName);
            }
        }

        /// <inheritdoc />
        public void UpdateWorkingDirectory(string directory)
        {
            WorkingDirectory = directory;
        }
    }
}
