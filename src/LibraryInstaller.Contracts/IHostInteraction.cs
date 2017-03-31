using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryInstaller.Contracts
{
    /// <summary>
    /// A class provided by the host to handle file writes etc.
    /// </summary>
    public interface IHostInteraction
    {
        /// <summary>
        /// The directory on disk the <see cref="IProvider"/> should use for caching purposes if caching is needed.
        /// </summary>
        /// <remarks>
        /// The cache directory is not being created, so each <see cref="IProvider"/> should ensure to do that if needed.
        /// </remarks>
        string CacheDirectory { get; }

        /// <summary>
        /// The root directory from where file paths are calculated.
        /// </summary>
        string WorkingDirectory { get; }

        /// <summary>
        /// Gets the logger associated with the host.
        /// </summary>
        ILogger Logger { get; }

        /// <summary>
        /// Writes a file to disk based on the specified <see cref="ILibraryInstallationState"/>.
        /// </summary>
        /// <param name="path">The full file path</param>
        /// <param name="content">The content of the file to write.</param>
        /// <param name="state">The desired state of the finished installed library.</param>
        /// <param name="cancellationToken">A token that allows cancellation of the file writing.</param>
        /// <returns><code>True</code> if no issues occured while executing this method; otherwise <code>False</code>.</returns>
        Task<bool> WriteFileAsync(string path, Func<Stream> content, ILibraryInstallationState state, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a file from disk.
        /// </summary>
        /// <param name="filePath">The absolute path to the file.</param>
        bool DeleteFile(string filePath);
    }
}
