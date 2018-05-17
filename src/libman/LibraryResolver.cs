// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Tools
{
    /// <summary>
    /// Provides a way to resolve libraries.
    /// </summary>
    internal static class LibraryResolver
    {
        /// <summary>
        /// Resolves libraries that match partial name
        /// </summary>
        /// <param name="partialName">Can be display name or library id.</param>
        /// <param name="manifest"></param>
        /// <param name="manifestDependencies"></param>
        /// <param name="provider"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<IReadOnlyList<ILibraryInstallationState>> ResolveAsync(
            string partialName,
            Manifest manifest, 
            IDependencies manifestDependencies,
            IProvider provider,
            CancellationToken cancellationToken)
        {
            var resolvedLibraries = new List<ILibraryInstallationState>();

            if (manifest?.Libraries == null || !manifest.Libraries.Any() || string.IsNullOrEmpty(partialName))
            {
                return resolvedLibraries;
            }

            Dictionary<string, ILibraryCatalog> catalogs = GetCatalogs(manifest, manifestDependencies, provider);

            var exactMatches = new List<ILibraryInstallationState>();

            foreach (KeyValuePair<string, ILibraryCatalog> catalog in catalogs)
            {
                bool exactMatchFound = false;
                try
                {
                    ILibrary lib = await catalog.Value.GetLibraryAsync(partialName, cancellationToken);
                    if (lib != null)
                    {
                        // Catalog returned a unique library id for the partial name. 
                        // If the installed libraries have the exact id for the given provider, then 
                        // we have found a match.

                        IEnumerable<ILibraryInstallationState> candidates = manifest.Libraries
                            .Where(l => l.LibraryId.Equals(partialName, StringComparison.OrdinalIgnoreCase)
                                && IsLibraryInstalledByProvider(l, catalog.Key, manifest.DefaultProvider));

                        if (candidates.Any())
                        {
                            exactMatchFound = true;
                            exactMatches.AddRange(candidates);
                        }

                        continue;
                        // No need to search for this provider.
                    }
                }
                catch
                {
                    // No Library matched exactly.
                }

                if (exactMatchFound)
                {
                    // If found any exact matches, then we should not perform
                    // search for this provider.
                    continue;
                }

                // For this provider, we did not find exact matches
                // Try to match on display name of search results.

                IReadOnlyList<ILibraryGroup> searchGroups = await catalog.Value.SearchAsync(partialName, 1, cancellationToken);
                IEnumerable<ILibraryInstallationState> matchedLibraries = await FindCandidatesFromSearchGroupAsync(
                    partialName,
                    manifest,
                    catalog.Key,
                    searchGroups,
                    cancellationToken);

                if (matchedLibraries.Any())
                {
                    resolvedLibraries.AddRange(matchedLibraries);
                }
            }

            exactMatches.AddRange(resolvedLibraries);

            return exactMatches;
        }

        /// <summary>
        /// Prompts the user to make a choice for the given libraries.
        /// </summary>
        /// <param name="installedLibraries"></param>
        /// <param name="hostEnvironment"></param>
        /// <returns></returns>
        public static ILibraryInstallationState ResolveLibraryByUserChoice(IEnumerable<ILibraryInstallationState> installedLibraries, IHostEnvironment hostEnvironment)
        {
            var sb = new StringBuilder(Resources.ChooseAnOption);
            sb.AppendLine();
            sb.Append('-', Resources.ChooseAnOption.Length);

            int index = 1;
            foreach (ILibraryInstallationState library in installedLibraries)
            {
                sb.Append($"{Environment.NewLine}{index}. {library.ToConsoleDisplayString()}");
                index++;
            }

            while (true)
            {
                string choice = hostEnvironment.InputReader.GetUserInput(sb.ToString());

                if (int.TryParse(choice, out int choiceIndex) && choiceIndex > 0 && choiceIndex < index)
                {
                    return installedLibraries.ElementAt(choiceIndex - 1);
                }
            }
        }

        private static async Task<IEnumerable<ILibraryInstallationState>> FindCandidatesFromSearchGroupAsync(
            string partialName,
            Manifest manifest,
            string providerId,
            IReadOnlyList<ILibraryGroup> searchGroups,
            CancellationToken cancellationToken)
        {
            var libraries = new List<ILibraryInstallationState>();
            if (searchGroups != null && searchGroups.Any())
            {
                foreach (ILibraryGroup group in searchGroups)
                {
                    if (group.DisplayName.Equals(partialName, StringComparison.OrdinalIgnoreCase))
                    {
                        IEnumerable<string> libraryIds = await group.GetLibraryIdsAsync(cancellationToken);

                        var libraryIdSet = libraryIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
                        IEnumerable<ILibraryInstallationState> candidates = manifest.Libraries
                            .Where(l => libraryIdSet.Contains(l.LibraryId) 
                                && IsLibraryInstalledByProvider(l, providerId, manifest.DefaultProvider));

                        if (candidates.Any())
                        {
                            libraries.AddRange(candidates);
                        }
                    }
                }
            }

            return libraries;
        }

        private static Dictionary<string, ILibraryCatalog> GetCatalogs(Manifest manifest, IDependencies manifestDependencies, IProvider provider)
        {
            var catalogs = new Dictionary<string, ILibraryCatalog>(StringComparer.OrdinalIgnoreCase);

            if (provider != null)
            {
                catalogs[provider.Id] = provider.GetCatalog();
            }
            else
            {
                var usedProviders = manifest.Libraries.Select(l => l.ProviderId).ToHashSet(StringComparer.OrdinalIgnoreCase);

                IEnumerable<IProvider> candidateProviders = manifestDependencies.Providers
                    .Where(p => manifest.DefaultProvider == p.Id || usedProviders.Contains(p.Id));

                foreach (IProvider p in manifestDependencies.Providers)
                {
                    catalogs[p.Id] = p.GetCatalog();
                }
            }

            return catalogs;
        }

        private static bool IsLibraryInstalledByProvider(ILibraryInstallationState l, string providerId, string defaultProvider)
        {
            return l.ProviderId == providerId || (string.IsNullOrEmpty(l.ProviderId) && defaultProvider == providerId);
        }
    }
}
