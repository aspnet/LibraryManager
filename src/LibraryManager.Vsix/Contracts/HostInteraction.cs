﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.Web.LibraryManager.Cache;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Contracts.Configuration;
using Microsoft.Web.LibraryManager.Vsix.Shared;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Web.LibraryManager.Vsix.Contracts
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
        public string CacheDirectory => CacheService.CacheFolder;
        public ILogger Logger { get; internal set; }

        /// <inheritdoc />
        public ISettings Settings => Configuration.Settings.DefaultSettings;

        public async Task<bool> WriteFileAsync(string relativePath, Func<Stream> content, ILibraryInstallationState state, CancellationToken cancellationToken)
        {
            FileInfo absolutePath = new FileInfo(Path.Combine(WorkingDirectory, relativePath));

            if (absolutePath.Exists)
            {
                return true;
            }

            // Note: using OrdinalIgnoreCase comparison as this code path is exclusive to Windows
            if (!absolutePath.FullName.StartsWith(WorkingDirectory, StringComparison.OrdinalIgnoreCase))
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

                await VsHelpers.CheckFileOutOfSourceControlAsync(absolutePath.FullName);
                await FileHelpers.SafeWriteToFileAsync(absolutePath.FullName, stream, cancellationToken);
            }

            Logger.Log(string.Format(LibraryManager.Resources.Text.FileWrittenToDisk, relativePath.Replace(Path.DirectorySeparatorChar, '/')), LogLevel.Operation);

            return true;
        }

        public async Task<bool> DeleteFilesAsync (IEnumerable<string> relativeFilePaths, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<string> absolutePaths = new List<string>();

            foreach (string filePath in relativeFilePaths)
            {
                string absoluteFilePath = Path.Combine(WorkingDirectory, filePath);
                FileInfo file = new FileInfo(absoluteFilePath);

                if (file.Exists)
                {
                    absolutePaths.Add(absoluteFilePath);
                }
            }

            //Delete from project
            Project project = VsHelpers.GetDTEProjectFromConfig(_configFilePath);
            bool isCpsProject = await VsHelpers.IsCpsProject(project);

            if (!isCpsProject)
            {
                var logAction = new Action<string, LogLevel>((message, level) => { Logger.Log(message, level); });
                bool deleteFromProject = await VsHelpers.DeleteFilesFromProjectAsync(project, absolutePaths, logAction, cancellationToken);
                if (deleteFromProject)
                {
                    return true;
                }
            }

            // Delete from file system 
            return await DeleteFilesFromDisk(absolutePaths, cancellationToken);

        }

        public Task<Stream> ReadFileAsync(string filePath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return FileHelpers.ReadFileAsStreamAsync(filePath, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> CopyFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string absoluteDestinationPath = Path.Combine(WorkingDirectory, destinationPath);
            if (!FileHelpers.IsUnderRootDirectory(absoluteDestinationPath, WorkingDirectory))
            {
                throw new UnauthorizedAccessException();
            }

            await VsHelpers.CheckFileOutOfSourceControlAsync(absoluteDestinationPath);
            bool result = await FileHelpers.CopyFileAsync(sourcePath, absoluteDestinationPath, cancellationToken);

            if (result)
            {
                Logger.Log(string.Format(LibraryManager.Resources.Text.FileWrittenToDisk, destinationPath.Replace(Path.DirectorySeparatorChar, '/')), LogLevel.Operation);
            }

            return result;
        }

        private Task<bool> DeleteFilesFromDisk(IEnumerable<string> filePaths, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                return FileHelpers.DeleteFiles(filePaths, WorkingDirectory);
            }, cancellationToken);
        }
    }
}
