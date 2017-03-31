using LibraryInstaller.Contracts;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryInstaller.Vsix
{
    public class HostInteraction : IHostInteraction
    {
        public HostInteraction(string configFilePath)
        {
            string cwd = Path.GetDirectoryName(configFilePath);
            WorkingDirectory = cwd;
        }

        public string WorkingDirectory { get; }
        public string CacheDirectory => Constants.CacheFolder;
        public ILogger Logger { get; } = new Logger();

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

                using (FileStream writer = File.Create(absolutePath))
                {
                    if (stream.CanSeek)
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                    }

                    await stream.CopyToAsync(writer, 8192, cancellationToken).ConfigureAwait(false);
                }
            }

            Logger.Log(string.Format(Resources.Text.FileWrittenToDisk, path.Replace('\\', '/')), Level.Operation);

            return true;
        }
    }
}
