// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Providers.FileSystem
{
    /// <summary>
    /// The <see cref="ILibraryCatalog"/> implementation for the <see cref="FileSystemProvider"/>.
    /// </summary>
    /// <seealso cref="Microsoft.Web.LibraryManager.Contracts.ILibraryCatalog" />
    internal class FileSystemCatalog : ILibraryCatalog
    {
        private readonly FileSystemProvider _provider;
        private readonly bool _underTest;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemCatalog"/> class.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="underTest">if set to <c>true</c> [under test].</param>
        public FileSystemCatalog(FileSystemProvider provider, bool underTest = false)
        {
            _provider = provider;
            _underTest = underTest;
        }

        /// <summary>
        /// Gets a list of completion spans for use in the JSON file.
        /// </summary>
        /// <param name="value">The current state of the library ID.</param>
        /// <param name="caretPosition">The caret position inside the <paramref name="value" />.</param>
        /// <returns></returns>
        public Task<CompletionSet> GetLibraryCompletionSetAsync(string value, int caretPosition)
        {
            if (value.Contains("://"))
            {
                return Task.FromResult(default(CompletionSet));
            }

            try
            {
                char separator = value.Contains('\\') ? '\\' : '/';
                int index = value.Length >= caretPosition - 1 ? value.LastIndexOf(separator, Math.Max(caretPosition - 1, 0)) : value.Length;
                string path = _provider.HostInteraction.WorkingDirectory;
                string prefix = "";

                if (index > 0)
                {
                    prefix = value.Substring(0, index + 1);
                    path = Path.Combine(path, prefix);
                }

                var set = new CompletionSet
                {
                    Start = 0,
                    Length = value.Length
                };

                var dir = new DirectoryInfo(path);

                if (dir.Exists)
                {
                    var list = new List<CompletionItem>();

                    foreach (FileSystemInfo item in dir.EnumerateDirectories())
                    {
                        var completion = new CompletionItem
                        {
                            DisplayText = item.Name + separator,
                            InsertionText = prefix + item.Name + separator,
                        };

                        list.Add(completion);
                    }

                    foreach (FileSystemInfo item in dir.EnumerateFiles())
                    {
                        var completion = new CompletionItem
                        {
                            DisplayText = item.Name,
                            InsertionText = prefix + item.Name,
                        };

                        list.Add(completion);
                    }

                    set.Completions = list;
                }

                return Task.FromResult(set);
            }
            catch (ArgumentException)
            {
                // Do not provide completion for invalid forms but allow user to type them.
                var set = new CompletionSet
                {
                    Start = 0,
                    Length = value.Length
                };

                return Task.FromResult(set);
            }
            catch
            {
                throw new InvalidLibraryException(value, _provider.Id);
            }
        }

        /// <summary>
        /// Gets the library group from the specified <paramref name="libraryName" />.
        /// </summary>
        /// <param name="libraryName">The name of the library.</param>
        /// <param name="version">Version of the library. (Ignored for FileSystemProvider)</param>
        /// <param name="cancellationToken">A token that allows the search to be cancelled.</param>
        /// <returns>
        /// An instance of <see cref="Microsoft.Web.LibraryManager.Contracts.ILibraryGroup" /> or <code>null</code>.
        /// </returns>
        public async Task<ILibrary> GetLibraryAsync(string libraryName, string version, CancellationToken cancellationToken)
        {
            ILibrary library;

            try
            {
                if (string.IsNullOrEmpty(libraryName) || libraryName.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                {
                    throw new InvalidLibraryException(libraryName, _provider.Id);
                }

                if (libraryName.Contains("://"))
                {
                    library = new FileSystemLibrary
                    {
                        Name = libraryName,
                        ProviderId = _provider.Id,
                        Files = await GetFilesAsync(libraryName).ConfigureAwait(false)
                    };

                    return library;
                }

                string path = Path.Combine(_provider.HostInteraction.WorkingDirectory, libraryName);

                if (!_underTest && !File.Exists(path) && !Directory.Exists(path))
                {
                    throw new InvalidLibraryException(libraryName, _provider.Id);
                }

                library = new FileSystemLibrary
                {
                    Name = libraryName,
                    ProviderId = _provider.Id,
                    Files = await GetFilesAsync(path).ConfigureAwait(false)
                };
            }
            catch (Exception)
            {
                throw new InvalidLibraryException(libraryName, _provider.Id);
            }

            return library;
        }

        private Task<IReadOnlyDictionary<string, bool>> GetFilesAsync(string libraryId)
        {
            return Task.Run<IReadOnlyDictionary<string, bool>>(() =>
            {
                if (Directory.Exists(libraryId))
                {
                    return Directory.EnumerateFiles(libraryId)
                            .Select(f => Path.GetFileName(f))
                            .ToDictionary((k) => k, (v) => true);
                }
                else
                {
                    return new Dictionary<string, bool>() { [Path.GetFileName(libraryId)] = true };
                }
            });
        }

        /// <summary>
        /// Searches the catalog for the specified search term.
        /// </summary>
        /// <param name="term">The search term.</param>
        /// <param name="maxHits">The maximum number of results to return.</param>
        /// <param name="cancellationToken">A token that allows the search to be cancelled.</param>
        /// <returns></returns>
        public Task<IReadOnlyList<ILibraryGroup>> SearchAsync(string term, int maxHits, CancellationToken cancellationToken)
        {
            var groups = new List<ILibraryGroup>()
            {
                new FileSystemLibraryGroup(term)
            };

            return Task.FromResult<IReadOnlyList<ILibraryGroup>>(groups);
        }

        /// <summary>
        /// Gets the latest version of the library.
        /// </summary>
        /// <param name="libraryId">The library identifier.</param>
        /// <param name="includePreReleases">if set to <c>true</c> includes pre-releases.</param>
        /// <param name="cancellationToken">A token that allows the search to be cancelled.</param>
        /// <returns>
        /// The library identifier of the latest released version.
        /// </returns>
        public Task<string> GetLatestVersion(string libraryId, bool includePreReleases, CancellationToken cancellationToken)
        {
            return Task.FromResult(string.Empty);
        }
    }
}
