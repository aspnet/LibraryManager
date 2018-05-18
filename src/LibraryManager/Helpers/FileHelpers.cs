// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager
{
    internal static class FileHelpers
    {
        public static async Task<bool> WriteToFileAsync(string fileName, Stream libraryStream, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            string directoryPath = Path.GetDirectoryName(fileName);

            if (directoryPath != null && !string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);

                using (FileStream destination = File.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    await libraryStream.CopyToAsync(destination);

                    return true;
                }
            }

            return false;
        }

        public static async Task<string> ReadFileTextAsync(string fileName, CancellationToken cancellationToken)
        {
            using (Stream s = await OpenFileAsync(fileName, cancellationToken).ConfigureAwait(false))
            using (var r = new StreamReader(s, Encoding.UTF8, true, 8192, true))
            {
                return await r.ReadToEndAsync().WithCancellation(cancellationToken).ConfigureAwait(false);
            }
        }

        public static Task<Stream> OpenFileAsync(string fileName, CancellationToken cancellationToken)
        {
            return Task.FromResult<Stream>(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1, useAsync: true));
        }
    }
}
