// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Tools.Contracts
{
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

        public string WorkingDirectory { get; private set; }
        public string CacheDirectory { get; }
        public ILogger Logger { get; }

        public async Task<bool> WriteFileAsync(string path, Func<Stream> content, ILibraryInstallationState state, CancellationToken cancellationToken)
        {
            var absolutePath = new FileInfo(Path.Combine(WorkingDirectory, path));

            if (absolutePath.Exists)
                return true;

            if (!absolutePath.FullName.StartsWith(WorkingDirectory))
                throw new UnauthorizedAccessException();

            if (absolutePath.Exists && (absolutePath.Attributes & FileAttributes.ReadOnly) != 0)
            {
                return true;
            }

            absolutePath.Directory.Create();

            using (Stream stream = content.Invoke())
            {
                if (stream == null)
                    return false;

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

        public void DeleteFiles(params string[] relativeFilePaths)
        {
            foreach (string relativeFilePath in relativeFilePaths)
            {
                string absoluteFile = Path.Combine(WorkingDirectory, relativeFilePath);

                try
                {
                    File.Delete(absoluteFile);

                    Logger.Log(string.Format(Resources.FileDeleted, relativeFilePath), LogLevel.Operation);
                }
                catch (Exception)
                {
                    Logger.Log(string.Format(Resources.FileDeleteFail, relativeFilePath), LogLevel.Operation);
                }
            }
        }

        public void UpdateWorkingDirectory(string directory)
        {
            WorkingDirectory = directory;
        }
    }
}