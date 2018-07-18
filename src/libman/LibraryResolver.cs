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
        public static IReadOnlyList<ILibraryInstallationState> ResolveByName(
            string libraryName,
            IEnumerable<ILibraryInstallationState> libraries,
            IDependencies dependenices,
            IProvider selectedProvider)
        {
            var resolvedLibraries = new List<ILibraryInstallationState>();

            if (libraries == null || !libraries.Any() || string.IsNullOrEmpty(libraryName))
            {
                return resolvedLibraries;
            }

            var nameMatches = new List<ILibraryInstallationState>();

            foreach (ILibraryInstallationState state in libraries)
            {
                if (selectedProvider != null && !state.ProviderId.Equals(selectedProvider.Id, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                IProvider provider = dependenices.GetProvider(state.ProviderId);
                if (provider == null)
                {
                    continue;
                }

                if (libraryName.Equals(provider.GetLibraryNameAndVersion(state.LibraryId).Name, StringComparison.OrdinalIgnoreCase))
                {
                    nameMatches.Add(state);
                }
            }

            return nameMatches;
        }

        /// <summary>
        /// Prompts the user to make a choice for the given libraries.
        /// </summary>
        /// <param name="installedLibraries"></param>
        /// <param name="hostEnvironment"></param>
        /// <returns></returns>
        public static ILibraryInstallationState ResolveLibraryByUserChoice(IEnumerable<ILibraryInstallationState> installedLibraries, IHostEnvironment hostEnvironment)
        {
            var sb = new StringBuilder(Resources.Text.ChooseAnOption);
            sb.AppendLine();
            sb.Append('-', Resources.Text.ChooseAnOption.Length);

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
