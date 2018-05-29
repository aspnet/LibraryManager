// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal class HostInteraction : IHostInteraction
    {
        private string _configFilePath;
        public HostInteraction(string configFilePath, ILogger logger)
        {
            string cwd = Path.GetDirectoryName(configFilePath);
            WorkingDirectory = cwd;
            Logger = logger;
            _configFilePath = configFilePath;
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
                {
                    return false;
                }

                VsHelpers.CheckFileOutOfSourceControl(absolutePath.FullName);
                await FileHelpers.WriteToFileAsync(absolutePath.FullName, stream, cancellationToken);
            }

            Logger.Log(string.Format(LibraryManager.Resources.Text.FileWrittenToDisk, relativePath.Replace(Path.DirectorySeparatorChar, '/')), LogLevel.Operation);

            return true;
        }

        public async Task<bool> DeleteFilesAsync (IEnumerable<string> relativeFilePaths, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<string> filePathsToDelete = new List<string>();

            foreach (string filePath in relativeFilePaths)
            {
                string absoluteFilePath = Path.Combine(WorkingDirectory, filePath);
                FileInfo file = new FileInfo(absoluteFilePath);

                if (file.Exists)
                {
                    filePathsToDelete.Add(absoluteFilePath);
                }
            }

            var logAction = new Action<string, LogLevel>((message, level) => { Logger.Log(message, level); });

            Project project = VsHelpers.GetDTEProjectFromConfig(_configFilePath);
            bool deleteFromProject = await VsHelpers.DeleteFilesFromProjectAsync(project, filePathsToDelete, logAction, cancellationToken);
            if (deleteFromProject)
            {
                return true;
            }

            return await DeleteFilesFromDiskAsync(relativeFilePaths, cancellationToken);

        }

        public Task<Stream> ReadFileAsync(string filePath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return FileHelpers.ReadFileAsStreamAsync(filePath, cancellationToken);
        }

        public Task<bool> CopyFile(string sourcePath, string destinationPath, CancellationToken cancellationToken)
        {
            return System.Threading.Tasks.Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                return FileHelpers.CopyFile(sourcePath, destinationPath);
            }, cancellationToken);
        }

        private Task<bool> DeleteFilesFromDiskAsync(IEnumerable<string> absoluteFilePaths, CancellationToken cancellationToken)
        {
            return System.Threading.Tasks.Task.Run(() => 
            {
                cancellationToken.ThrowIfCancellationRequested();

                HashSet<string> directories = new HashSet<string>();

                try
                {
                    foreach (string absoluteFilePath in absoluteFilePaths)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (File.Exists(absoluteFilePath))
                        {
                            FileHelpers.DeleteFileFromDisk(absoluteFilePath);
                        }

                        string directoryPath = Path.GetDirectoryName(absoluteFilePath);
                        if (Directory.Exists(directoryPath))
                        {
                            if (!directories.Contains(directoryPath))
                            {
                                directories.Add(directoryPath);
                                // TO DO : replace for DeleteFolder that also calls 
                                // DeleteFolderFromProject as needed
                            }
                        }
                    }

                    FileHelpers.DeleteEmptyFoldersFromDisk(directories);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }

            }, cancellationToken);
        }
    }
}
