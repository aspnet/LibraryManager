// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryManager.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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

            if (absolutePath.Exists && (absolutePath.Attributes & FileAttributes.ReadOnly) != 0)
            {
                return true;
            }

            absolutePath.Directory.Create();

            using (Stream stream = content.Invoke())
            {
                if (stream == null || !await WriteToFileAsync(absolutePath.FullName, stream).ConfigureAwait(false))
                {
                    return false;
                }
            }

            Logger.Log(string.Format(Resources.Text.FileWrittenToDisk, path.Replace('\\', '/')), LogLevel.Operation);

            return true;
        }

        internal static async Task<bool> WriteToFileAsync(string fileName, Stream libraryStream)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));

                using (FileStream destination = File.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    await libraryStream.CopyToAsync(destination);
                }

                return true;
            }
            catch(Exception)
            {
                return false;
            }
        }

        public Task<bool> DeleteFilesAsync(IEnumerable<string> relativeFilePaths, CancellationToken cancellationToken, bool deleteCleanFolders = true)
        {
            throw new NotImplementedException();
        }
    }
}
