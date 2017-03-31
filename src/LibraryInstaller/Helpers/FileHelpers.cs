using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryInstaller
{
    internal static class FileHelpers
    {
        public static async Task<string> GetFileTextAsync(string url, string localFile, int expiresAfterDays, CancellationToken cancellationToken)
        {
            Stream s = await GetFileAsync(url, localFile, expiresAfterDays, cancellationToken).ConfigureAwait(false);

            if (s != null)
            {
                using (s)
                using (var r = new StreamReader(s, Encoding.UTF8, true, 8192, true))
                {
                    return await r.ReadToEndAsync().WithCancellation(cancellationToken).ConfigureAwait(false);
                }
            }

            return null;
        }

        public static async Task<Stream> GetFileAsync(string url, string localFile, int expiresAfterDays, CancellationToken cancellationToken)
        {
            if (!File.Exists(localFile) || File.GetLastWriteTime(localFile) < DateTime.Now.AddDays(-expiresAfterDays))
            {
                return await DownloadFileAsync(url, localFile, cancellationToken).ConfigureAwait(false);
            }

            return await ReadFileAsync(localFile, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<Stream> DownloadFileAsync(string url, string fileName, CancellationToken cancellationToken)
        {
            Stream content = null;

            try
            {
                using (var client = new HttpClient())
                {
                    content = await client.GetStreamAsync(url).WithCancellation(cancellationToken).ConfigureAwait(false);

                    Directory.CreateDirectory(Path.GetDirectoryName(fileName));

                    using (FileStream f = File.Create(fileName))
                    {
                        content.CopyTo(f);
                        await f.FlushAsync(cancellationToken).ConfigureAwait(false);
                    }

                    return File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex);
            }

            return content;
        }

        public static async Task<string> ReadFileTextAsync(string fileName, CancellationToken cancellationToken)
        {
            using (Stream s = await ReadFileAsync(fileName, cancellationToken).ConfigureAwait(false))
            using (var r = new StreamReader(s, Encoding.UTF8, true, 8192, true))
            {
                return await r.ReadToEndAsync().WithCancellation(cancellationToken).ConfigureAwait(false);
            }
        }

        public static Task<Stream> ReadFileAsync(string fileName, CancellationToken cancellationToken)
        {
            return Task.FromResult<Stream>(File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read));
        }
    }
}
