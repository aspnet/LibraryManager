// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using LibraryInstaller.Contracts;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryInstaller.Build
{
    public class HostInteraction : IHostInteraction
    {
        public HostInteraction(RestoreTask task)
        {
            string cwd = Path.GetDirectoryName(task.FileName);
            WorkingDirectory = cwd;
            Logger = new Logger(task);
        }

        public string WorkingDirectory { get; }
        public string CacheDirectory => Constants.CacheFolder;
        public ILogger Logger { get; }

        public async Task<bool> WriteFileAsync(string path, Func<Stream> content, ILibraryInstallationState reqestor, CancellationToken cancellationToken)
        {
            string absolutePath = Path.Combine(WorkingDirectory, path);

            if (File.Exists(absolutePath))
                return true;

            string directory = Path.GetDirectoryName(absolutePath);

            Directory.CreateDirectory(directory);

            using (Stream stream = content.Invoke())
            {
                if (stream == null)
                    return false;

                using (FileStream writer = File.Create(absolutePath, 4096, FileOptions.Asynchronous))
                {
                    if (stream.CanSeek)
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                    }

                    await stream.CopyToAsync(writer, 8192, cancellationToken).ConfigureAwait(false);
                }
            }

            Logger.Log(string.Format(Resources.Text.FileWrittenToDisk, path.Replace('\\', '/')), LogLevel.Operation);

            return true;
        }

        public void DeleteFiles(params string[] relativeFilePaths)
        {
            foreach (string relativeFilePath in relativeFilePaths)
            {
                string absoluteFile = Path.Combine(WorkingDirectory, relativeFilePath);

                try
                {
                    File.Delete(absoluteFile);

                    Logger.Log(string.Format(Resources.Text.FileDeleted, relativeFilePath), LogLevel.Operation);
                }
                catch (Exception)
                {
                    Logger.Log(string.Format(Resources.Text.FileDeleteFail, relativeFilePath), LogLevel.Operation);
                }
            }
        }
    }
}
