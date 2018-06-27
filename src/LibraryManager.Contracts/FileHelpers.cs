// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Contracts
{
    /// <summary>
    /// Helper class for basic read and write file operations 
    /// </summary>
    public static class FileHelpers
    {
        /// <summary>
        /// Writes the stream to a temporary file first, then moves the temporary file to the destination file
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="sourceStream"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<bool> SafeWriteToFileAsync(string fileName, Stream sourceStream, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string tempFileName = Path.GetTempFileName();

            bool result = true;

            if (string.IsNullOrWhiteSpace(tempFileName))
            {
                Debug.Fail($"Unexpected: null or empty {nameof(tempFileName)}");

                // If we are on a platform where Path.GetTempFileName() isn't implemented and returns null or empty string,
                // fallback to just writing directly to the destination file
                result = await WriteToFileAsync(fileName, sourceStream, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                result = await WriteToFileAsync(tempFileName, sourceStream, cancellationToken).ConfigureAwait(false);

                if (result)
                {
                    result = await MoveFileAsync(tempFileName, fileName);
                }

                // Clean up temp file if we didn't move it to the desination file successfully
                if (!result)
                {
                    try
                    {
                        DeleteFiles(new string[] { tempFileName });
                    }
                    catch
                    {
                        Debug.Fail($"Could not clean up temporary file {tempFileName}");
                        // Don't fail the operation if we couldn't clean up temporary file
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Writes a Stream to a destination file
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="sourceStream"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task<bool> WriteToFileAsync(string fileName, Stream sourceStream, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string directoryPath = Path.GetDirectoryName(fileName);

            if (!string.IsNullOrEmpty(directoryPath))
            {
                DirectoryInfo dir = Directory.CreateDirectory(directoryPath);
                using (FileStream destination = File.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await sourceStream.CopyToAsync(destination);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Reads from a file a returns its content as text
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<string> ReadFileAsTextAsync(string fileName, CancellationToken cancellationToken)
        {
            using (Stream s = await ReadFileAsStreamAsync(fileName, cancellationToken).ConfigureAwait(false))
            using (var r = new StreamReader(s, Encoding.UTF8, true, 8192, true))
            {
                return await r.ReadToEndAsync().WithCancellation(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Reads from a file a returns its content as Stream
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task<Stream> ReadFileAsStreamAsync(string fileName, CancellationToken cancellationToken)
        {
            return Task.FromResult<Stream>(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, 1, useAsync: true));
        }

        /// <summary>
        /// Copies a file from source to destination
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="destinationFile"></param>
        /// <returns></returns>
        public static bool CopyFile(string sourceFile, string destinationFile)
        {
            try
            {
                File.Copy(sourceFile, destinationFile, true);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Moves source file to destinatino file asynchronously
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="destinationFile"></param>
        /// <returns></returns>
        public static async Task<bool> MoveFileAsync(string sourceFile, string destinationFile)
        {
            bool result = true;

            if (string.IsNullOrEmpty(sourceFile) || string.IsNullOrEmpty(destinationFile) || !File.Exists(sourceFile))
            {
                result = false;
            }

            if (result)
            {
                try
                {
                    await Task.Run(() =>
                    {
                        if (File.Exists(destinationFile))
                        {
                            File.Delete(destinationFile);
                        }

                        string directoryName = Path.GetDirectoryName(destinationFile);
                        Directory.CreateDirectory(directoryName);

                        File.Move(sourceFile, destinationFile);
                    }
                ).ConfigureAwait(false);

                }
                catch (Exception)
                {
                    result = false;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns true if both files have same same, size and last written time
        /// </summary>
        /// <param name="firstFile"></param>
        /// <param name="secondFile"></param>
        /// <returns></returns>
        public static bool AreFilesUpToDate(FileInfo firstFile, FileInfo secondFile)
        {
            if (firstFile == null || secondFile == null || !firstFile.Exists || !secondFile.Exists)
            {
                return false;
            }

            if (string.Compare(firstFile.Name, secondFile.Name, true) != 0 ||
                secondFile.Length != firstFile.Length ||
                secondFile.LastWriteTime.CompareTo(firstFile.LastWriteTime) > 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Deletes files 
        /// </summary>
        /// <param name="filePaths"></param>
        /// <returns></returns>
        public static bool DeleteFiles(IEnumerable<string> filePaths)
        {
            HashSet<string> directories = new HashSet<string>();

            try
            {
                foreach (string filePath in filePaths)
                {
                    if (File.Exists(filePath))
                    {
                        DeleteFileFromDisk(filePath);
                    }

                    string directoryPath = Path.GetDirectoryName(filePath);
                    if (Directory.Exists(directoryPath))
                    {
                        directories.Add(directoryPath);
                    }
                }

                DeleteEmptyFoldersFromDisk(directories);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Deletes folders if empty
        /// </summary>
        /// <param name="folderPaths"></param>
        /// <returns></returns>
        public static bool DeleteEmptyFoldersFromDisk(IEnumerable<string> folderPaths)
        {
            try
            {
                foreach (string path in folderPaths)
                {
                    DirectoryInfo directory = new DirectoryInfo(path);
                    if (directory.Exists && IsDirectoryEmpty(path))
                    {
                        directory.Delete(true);
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true is directory is empty
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsDirectoryEmpty(string path)
        {
            if (Directory.Exists(path))
            {
                return !Directory.EnumerateFileSystemEntries(path).Any();
            }

            return false;
        }

        /// <summary>
        /// Deletes a file from disk if exists
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static bool DeleteFileFromDisk(string filePath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                if (fileInfo.Exists)
                {
                    File.Delete(filePath);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
