// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryInstaller.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Microsoft.Web.LibraryInstaller.Providers.Cdnjs
{
    internal class CdnjsProvider : IProvider
    {
        private const string _downloadUrlFormat = "https://cdnjs.cloudflare.com/ajax/libs/{0}/{1}/{2}";
        private CdnjsCatalog _catalog;

        public CdnjsProvider(IHostInteraction hostInteraction)
        {
            HostInteraction = hostInteraction;
        }

        public string Id { get; } = "cdnjs";

        public IHostInteraction HostInteraction { get; }

        internal string CacheFolder
        {
            get { return Path.Combine(HostInteraction.CacheDirectory, Id); }
        }

        public ILibraryCatalog GetCatalog()
        {
            if (_catalog == null)
            {
                _catalog = new CdnjsCatalog(this);
            }

            return _catalog;
        }

        public async Task<ILibraryInstallationResult> InstallAsync(ILibraryInstallationState desiredState, CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return LibraryInstallationResult.FromCancelled(desiredState);
                }

                var catalog = (CdnjsCatalog)GetCatalog();
                ILibrary library = await catalog.GetLibraryAsync(desiredState.LibraryId, cancellationToken).ConfigureAwait(false);

                if (library == null)
                {
                    throw new InvalidLibraryException(desiredState.LibraryId, Id);
                }

                await HydrateCacheAsync(library, cancellationToken).ConfigureAwait(false);

                var files = desiredState.Files?.ToList();
                // "Files" is optional on this provider, so when none are specified all should be used
                if (files == null || files.Count == 0)
                {
                    desiredState = new LibraryInstallationState
                    {
                        ProviderId = Id,
                        LibraryId = desiredState.LibraryId,
                        DestinationPath = desiredState.DestinationPath,
                        Files = library.Files.Keys.ToList(),
                    };
                }

                foreach (string file in desiredState.Files)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return LibraryInstallationResult.FromCancelled(desiredState);
                    }

                    string path = Path.Combine(desiredState.DestinationPath, file);
                    var sourceStream = new Func<Stream>(() => GetStreamAsync(desiredState, file, cancellationToken).Result);
                    bool writeOk = await HostInteraction.WriteFileAsync(path, sourceStream, desiredState, cancellationToken).ConfigureAwait(false);

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

        private async Task<Stream> GetStreamAsync(ILibraryInstallationState state, string sourceFile, CancellationToken cancellationToken)
        {
            string[] args = state.LibraryId.Split('@');
            string name = args[0];
            string version = args[1];
            string absolute = Path.Combine(CacheFolder, name, version, sourceFile);

            if (File.Exists(absolute))
            {
                return await FileHelpers.OpenFileAsync(absolute, cancellationToken);
            }

            return null;
        }

        public async Task HydrateCacheAsync(ILibrary library, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            string libraryDir = Path.Combine(CacheFolder, library.Name);
            var tasks = new List<Task>();

            foreach (string file in library.Files.Keys)
            {
                string localFile = Path.Combine(libraryDir, library.Version, file);

                if (!File.Exists(localFile))
                {
                    string url = string.Format(_downloadUrlFormat, library.Name, library.Version, file);
                    Task<string> task = FileHelpers.GetFileTextAsync(url, localFile, 0, cancellationToken);
                    tasks.Add(task);
                }
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
