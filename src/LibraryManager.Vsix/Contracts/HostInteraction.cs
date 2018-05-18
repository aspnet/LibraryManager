// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal class HostInteraction : IHostInteraction
    {
        public HostInteraction(string configFilePath, ILogger logger)
        {
            string cwd = Path.GetDirectoryName(configFilePath);
            WorkingDirectory = cwd;
            Logger = logger;
        }

        public string WorkingDirectory { get; }
        public string CacheDirectory => Constants.CacheFolder;
        public ILogger Logger { get; internal set; }

        public async Task<bool> WriteFileAsync(string relativePath, Func<Stream> content, ILibraryInstallationState state, CancellationToken cancellationToken)
        {
            FileInfo absolutePath = new FileInfo(Path.Combine(WorkingDirectory, relativePath));

            if (absolutePath.Exists)
            {
                return true;
            }

            if (!absolutePath.FullName.StartsWith(WorkingDirectory))
            {
                throw new UnauthorizedAccessException();
            }

            absolutePath.Directory.Create();

            using (Stream stream = content.Invoke())
            {
                if (stream == null)
                    return false;

                VsHelpers.CheckFileOutOfSourceControl(absolutePath.FullName);

                using (FileStream writer = File.Create(absolutePath.FullName, 4096, FileOptions.Asynchronous))
                {
                    if (stream.CanSeek)
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                    }

                    await stream.CopyToAsync(writer, 8192, cancellationToken).ConfigureAwait(false);
                }
            }

            Logger.Log(string.Format(LibraryManager.Resources.Text.FileWrittenToDisk, relativePath.Replace(Path.DirectorySeparatorChar, '/')), LogLevel.Operation);

            return true;
        }

        public async Task<bool> DeleteFilesAsync(IEnumerable<string> relativeFilePaths, CancellationToken cancellationToken, bool deleteCleanFolders = true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            List<Task<bool>> deleteFilesTasks = new List<Task<bool>>();
            List<Task<bool>> deleteFoldersTasks = new List<Task<bool>>();
            HashSet<string> directories = new HashSet<string>();

            foreach (string relativeFilePath in relativeFilePaths)
            {
                string absoluteFile = new FileInfo(Path.Combine(WorkingDirectory, relativeFilePath)).FullName;

                if (cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                if (File.Exists(absoluteFile))
                {
                    deleteFilesTasks.Add(DeleteFileAsync(absoluteFile));
                }

                if (deleteCleanFolders)
                {
                    string directoryPath = Path.GetDirectoryName(absoluteFile);
                    if (Directory.Exists(directoryPath))
                    {
                        if (!directories.Contains(directoryPath))
                        {
                            directories.Add(directoryPath);
                            // TO DO : replace for DeleteFolder that also calls 
                            // DeleteFolderFromProject as needed
                            deleteFoldersTasks.Add(DeleteFolderFromDisk(directoryPath));
                        }
                    }
                }
            }

            await Task.WhenAll(deleteFilesTasks);

            if (deleteCleanFolders)
            {
                await Task.WhenAll(deleteFoldersTasks);
            }

            return deleteFilesTasks.All(t => t.Result);
        }

        // TO DO: Move to the FileSystemHelpers
        private async Task<bool> DeleteFileAsync(string filePath)
        {
            ProjectItem item = VsHelpers.DTE.Solution.FindProjectItem(filePath);
            Project project = item?.ContainingProject;
            bool deleteSucceeded = false;

            if (project != null)
            {
                deleteSucceeded = await DeleteFileFromProject(item);
            }
            else
            {
                deleteSucceeded = await DeleteFileFromDisk(filePath);
            }

            if (deleteSucceeded)
            {
                Logger.Log(string.Format(LibraryManager.Resources.Text.FileDeleted, filePath.Replace(Path.DirectorySeparatorChar, '/')), LogLevel.Operation);
            }
            else
            {
                Logger.Log(string.Format(LibraryManager.Resources.Text.FileDeleteFail, filePath.Replace(Path.DirectorySeparatorChar, '/')), LogLevel.Operation);
            }

            return deleteSucceeded;
        }
        private Task<bool> DeleteFolderFromDisk (string folderPath)
        {
            return Task.Run(() =>
            {
                try
                {
                    DirectoryInfo directory = new DirectoryInfo(folderPath);
                    if (directory.Exists && IsDirectoryEmpty(folderPath))
                    {
                        directory.Delete(true);
                    }

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            });

        }

        private bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        private Task<bool> DeleteFileFromDisk(string filePath)
        {
            return Task.Run(() =>
            {
                try
                {
                    FileInfo fileInfo = new FileInfo(filePath);
                    if (fileInfo.Exists)
                    {
                        VsHelpers.CheckFileOutOfSourceControl(filePath);
                        File.Delete(filePath);
                    }

                    return true;
                }
                catch (Exception)
                {
                    // Add telemetry here
                    return false;
                }
            });
        }

        // TO DO: Move to VS helpers
        private Task<bool> DeleteFileFromProject(ProjectItem projectItem)
        {
            return Task.Run(() =>
            {
                if (projectItem != null)
                {
                    try
                    {
                        projectItem.Delete();
                        return true;
                    }
                    catch (Exception)
                    {
                        // TO DO: log Error 
                        return false;
                    }
                }

                return false;
            });
        }

        // TO DO: Move to the FileSystemHelpers
        private bool IsFileUpToDate(FileInfo cacheFile, FileInfo destinationFile)
        {
            if (cacheFile.Length != destinationFile.Length || cacheFile.LastWriteTime.CompareTo(destinationFile.LastWriteTime) > 0)
            {
                return false;
            }

            return true;
        }
    }
}
