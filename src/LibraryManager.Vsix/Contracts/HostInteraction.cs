// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
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

        public async Task<bool> WriteFileAsync(string path, Func<Stream> content, ILibraryInstallationState state, CancellationToken cancellationToken)
        {
            var absolutePath = new FileInfo(Path.Combine(WorkingDirectory, path));

            if (absolutePath.Exists)
                return true;

            if (!absolutePath.FullName.StartsWith(WorkingDirectory))
                throw new UnauthorizedAccessException();

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

            Logger.Log(string.Format(LibraryManager.Resources.Text.FileWrittenToDisk, path.Replace(Path.DirectorySeparatorChar, '/')), LogLevel.Operation);

            return true;
        }

        public void DeleteFiles(params string[] relativeFilePaths)
        {
            foreach (string relativeFilePath in relativeFilePaths)
            {
                string absoluteFile = new FileInfo(Path.Combine(WorkingDirectory, relativeFilePath)).FullName;

                try
                {
                    ProjectItem item = VsHelpers.DTE.Solution.FindProjectItem(absoluteFile);
                    Project project = item?.ContainingProject;

                    if (project != null)
                    {
                        item.Delete();
                    }
                    else
                    {
                        if (File.Exists(absoluteFile))
                        {
                            VsHelpers.CheckFileOutOfSourceControl(absoluteFile);
                            File.Delete(absoluteFile);
                        }
                    }

                    Logger.Log(string.Format(LibraryManager.Resources.Text.FileDeleted, relativeFilePath.Replace(Path.DirectorySeparatorChar, '/')), LogLevel.Operation);
                }
                catch (Exception ex)
                {
                    Logger.Log(string.Format(LibraryManager.Resources.Text.FileDeleteFail, relativeFilePath.Replace(Path.DirectorySeparatorChar, '/')), LogLevel.Operation);
                    Telemetry.TrackException("deletefilefailed", ex);
                }
            }
        }
    }
}
