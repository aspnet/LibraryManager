// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using LibraryInstaller.Contracts;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryInstaller.Providers.FileSystem
{
    internal class FileSystemProvider : IProvider
    {
        private FileSystemCatalog _catalog;

        public string Id => "filesystem";

        public IHostInteraction HostInteraction
        {
            get;
            set;
        }

        public ILibraryCatalog GetCatalog()
        {
            if (_catalog == null)
            {
                _catalog = new FileSystemCatalog(Id);
            }

            return _catalog;
        }

        public async Task<ILibraryInstallationResult> InstallAsync(ILibraryInstallationState desiredState, CancellationToken cancellationToken)
        {
            try
            {
                foreach (string file in desiredState.Files)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return LibraryInstallationResult.FromCancelled(desiredState);
                    }

                    string path = Path.Combine(desiredState.DestinationPath, file);
                    var func = new Func<Stream>(() => GetStreamAsync(desiredState, file, cancellationToken).Result);
                    bool writeOk = await HostInteraction.WriteFileAsync(path, func, desiredState, cancellationToken).ConfigureAwait(false);

                    if (!writeOk)
                    {
                        return new LibraryInstallationResult(desiredState, PredefinedErrors.CouldNotWriteFile(file));
                    }
                }
            }
            catch (Exception ex) when (ex is InvalidLibraryException || ex.InnerException is InvalidLibraryException)
            {
                return new LibraryInstallationResult(desiredState, PredefinedErrors.UnableToResolveSource(desiredState.LibraryId, desiredState.ProviderId));
            }
            catch (Exception ex)
            {
                HostInteraction.Logger.Log(ex.ToString(), LogLevel.Error);
                return new LibraryInstallationResult(desiredState, PredefinedErrors.UnknownException());
            }

            return LibraryInstallationResult.FromSuccess(desiredState);
        }

        private async Task<Stream> GetStreamAsync(ILibraryInstallationState state, string file, CancellationToken cancellationToken)
        {
            string sourceFile = state.LibraryId;

            try
            {
                if (!Uri.TryCreate(sourceFile, UriKind.RelativeOrAbsolute, out Uri url))
                    return null;

                if (!url.IsAbsoluteUri)
                {
                    sourceFile = new FileInfo(Path.Combine(HostInteraction.WorkingDirectory, sourceFile)).FullName;
                    if (!Uri.TryCreate(sourceFile, UriKind.Absolute, out url))
                        return null;
                }

                // File
                if (url.IsFile)
                {
                    if (Directory.Exists(url.OriginalString))
                    {
                        return await FileHelpers.ReadFileAsync(Path.Combine(url.OriginalString, file), cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        return await FileHelpers.ReadFileAsync(sourceFile, cancellationToken).ConfigureAwait(false);
                    }
                }
                // Url
                else
                {
                    var client = new HttpClient();
                    return await client.GetStreamAsync(sourceFile).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                throw new InvalidLibraryException(state.LibraryId, state.ProviderId);
            }
        }
    }
}