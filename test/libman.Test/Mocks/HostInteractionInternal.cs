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
using Microsoft.Web.LibraryManager.Tools.Contracts;

namespace Microsoft.Web.LibraryManager.Tools.Test.Mocks
{
    internal class HostInteractionInternal : IHostInteractionInternal
    {
        public HostInteractionInternal(string workingDirectory, string cacheDirectory)
        {
            WorkingDirectory = workingDirectory;
            CacheDirectory = cacheDirectory;

            if (!string.IsNullOrEmpty(WorkingDirectory))
            {
                Directory.CreateDirectory(WorkingDirectory);
            }
        }

        public string CacheDirectory { get; set; }

        public string WorkingDirectory { get; set; }

        public ILogger Logger { get; set; }

        public ISettings Settings { get; set; }

        public Task<bool> CopyFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
        {
            if (File.Exists(sourcePath))
            {
                if (!Path.IsPathRooted(destinationPath))
                {
                    destinationPath = Path.Combine(WorkingDirectory, destinationPath);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                File.Copy(sourcePath, destinationPath, overwrite: true);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<bool> DeleteFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken)
        {

            IEnumerable<string> fullPaths = filePaths.Select(f => Path.Combine(WorkingDirectory, f));
            bool result = FileHelpers.DeleteFiles(fullPaths, WorkingDirectory);

            return Task.FromResult(result);
        }

        public Task<Stream> ReadFileAsync(string filePath, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void UpdateWorkingDirectory(string directory)
        {
            throw new NotImplementedException();
        }

        public Task<bool> WriteFileAsync(string filePath, Func<Stream> content, ILibraryInstallationState state, CancellationToken cancellationToken)
        {
            string path = Path.Combine(WorkingDirectory, filePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.Create(path).Dispose();
            return Task.FromResult(true);
        }
    }
}
