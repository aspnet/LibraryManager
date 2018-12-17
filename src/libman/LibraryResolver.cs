// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.LibraryNaming;

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
        /// <param name="provider"></param>
        /// <returns></returns>
        public static IReadOnlyList<ILibraryInstallationState> Resolve(
            string partialName,
            Manifest manifest,
            IProvider provider)
        {
            var resolvedLibraries = new List<ILibraryInstallationState>();

            if (manifest?.Libraries == null || !manifest.Libraries.Any() || string.IsNullOrEmpty(partialName))
            {
                return resolvedLibraries;
            }

            var idMatches = new List<ILibraryInstallationState>();
            var nameMatches = new List<ILibraryInstallationState>();

            foreach(ILibraryInstallationState state in manifest.Libraries)
            {
                if (provider != null && !state.ProviderId.Equals(provider.Id, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string libraryId = LibraryIdToNameAndVersionConverter.Instance.GetLibraryId(
                                        state.Name,
                                        state.Version,
                                        state.ProviderId);

                if (libraryId.Equals(partialName, StringComparison.OrdinalIgnoreCase))
                {
                    idMatches.Add(state);
                }
                else if (state.Name.Equals(partialName, StringComparison.OrdinalIgnoreCase))
                {
                    nameMatches.Add(state);
                }
            }

            // Maintain ordering of id matches before name matches.
            idMatches.AddRange(nameMatches);

            return idMatches;
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
                string choice = string.Empty;
                try
                {
                    choice = hostEnvironment.InputReader.GetUserInput(sb.ToString());
                }
                catch (InvalidOperationException)
                {
                    hostEnvironment.Logger.Log(Resources.Text.SpecifyDisabmiguatedLibrary, LogLevel.Error);
                    throw;
                }

                if (int.TryParse(choice, out int choiceIndex) && choiceIndex > 0 && choiceIndex < index)
                {
                    return installedLibraries.ElementAt(choiceIndex - 1);
                }
            }
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
