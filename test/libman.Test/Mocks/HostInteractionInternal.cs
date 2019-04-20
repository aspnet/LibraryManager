// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Contracts.Configuration;
using Microsoft.Web.LibraryManager.Tools.Contracts;

namespace Microsoft.Web.LibraryManager.Tools.Test.Mocks
{
    internal class HostInteractionInternal : IHostInteractionInternal
    {
        public string CacheDirectory => throw new NotImplementedException();

        public string WorkingDirectory => throw new NotImplementedException();

        public ILogger Logger { get; set; }

        public ISettings Settings { get; set; }

        public Task<bool> CopyFile(string sourcePath, string destinationPath, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
    }
}
