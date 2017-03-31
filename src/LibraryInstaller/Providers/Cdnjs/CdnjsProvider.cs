// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using LibraryInstaller.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryInstaller.Providers.Cdnjs
{
    internal class CdnjsProvider : IProvider
    {
        private const string _downloadUrlFormat = "https://cdnjs.cloudflare.com/ajax/libs/{0}/{1}/{2}";
        private CdnjsCatalog _catalog;

        public string Id => "cdnjs";
        public IHostInteraction HostInteraction
        {
            get;
            set;
        }

        private string CacheFolder
        {
            get { return Path.Combine(HostInteraction.CacheDirectory, Id); }
        }

        public ILibraryCatalog GetCatalog()
        {
            if (_catalog == null)
            {
                _catalog = new CdnjsCatalog(CacheFolder, Id);
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
                ILibrary pkg = await catalog.GetLibraryAsync(desiredState.LibraryId, cancellationToken).ConfigureAwait(false);

                if (pkg == null)
                {
                    throw new InvalidLibraryException(desiredState.LibraryId, Id);
                }

                await HydrateCacheAsync(pkg, cancellationToken).ConfigureAwait(false);

                foreach (string file in desiredState.Files)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return LibraryInstallationResult.FromCancelled(desiredState);
                    }

                    string path = Path.Combine(desiredState.Path, file);
                    var func = new Func<Stream>(() => GetStream(desiredState, file));
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
                HostInteraction.Logger.Log(ex.ToString(), Level.Error);
                return new LibraryInstallationResult(desiredState, PredefinedErrors.UnknownException());
            }

            return LibraryInstallationResult.FromSuccess(desiredState);
        }

        private Stream GetStream(ILibraryInstallationState state, string sourceFile)
        {
            string[] args = state.LibraryId.Split('@');
            string name = args[0];
            string version = args[1];
            string absolute = Path.Combine(CacheFolder, name, version, sourceFile);

            if (File.Exists(absolute))
            {
                return File.Open(absolute, FileMode.Open, FileAccess.Read);
            }

            return null;
        }

        public async Task HydrateCacheAsync(ILibrary library, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            string libraryDir = Path.Combine(CacheFolder, library.Id);
            var tasks = new List<Task>();

            foreach (string file in library.Files.Keys)
            {
                string localFile = Path.Combine(libraryDir, library.Version, file);

                if (!File.Exists(localFile))
                {
                    string url = string.Format(_downloadUrlFormat, library.Id, library.Version, file);
                    Task<string> task = FileHelpers.GetFileTextAsync(url, localFile, 0, cancellationToken);
                    tasks.Add(task);
                }
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
