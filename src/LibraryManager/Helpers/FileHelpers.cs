// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager
{
    internal static class FileHelpers
    {
<<<<<<< HEAD
        public static async Task<string> GetFileTextAsync(string url, string localFile, int expiresAfterDays, CancellationToken cancellationToken)
        {
            if (!File.Exists(localFile) || File.GetLastWriteTime(localFile) < DateTime.Now.AddDays(-expiresAfterDays))
            {
                await DownloadFileAsync(url, localFile, cancellationToken).ConfigureAwait(false);
            }

            return await ReadFileTextAsync(localFile, cancellationToken).ConfigureAwait(false);
        }

=======
>>>>>>> Fixing issues with restore and cache management.
        public static async Task DownloadFileAsync(string url, string fileName, CancellationToken cancellationToken)
        {
            Stream content = null;

            using (var client = new HttpClient())
            {
                try
                {
                    content = await client.GetStreamAsync(url).WithCancellation(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Add telemetry here for failures
                    throw new ResourceDownloadException(url);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(fileName));

                using (FileStream f = File.Create(fileName))
                {
                    content.CopyTo(f);
                    await f.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
            }
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
            return Task.FromResult<Stream>(File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read));
        }
    }
}
