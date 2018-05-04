// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Tools.Commands
{
    internal class UpdateCommand : BaseCommand
    {
        public UpdateCommand(IHostEnvironment environment, bool throwOnUnexpectedArg=true)
            : base(throwOnUnexpectedArg, "update", Resources.UpdateCommandDesc, environment)
        {
        }

        public override string Remarks => Resources.UpdateCommandRemarks;

        public override string Examples => Resources.UpdateCommandExamples;

        public CommandArgument LibraryId { get; private set; }
        public CommandOption Provider { get; private set; }
        public CommandOption PreRelease { get; private set; }

        public override BaseCommand Configure(CommandLineApplication parent = null)
        {
            base.Configure(parent);

            LibraryId = Argument("libraryId", Resources.UpdateCommandLibraryArgumentDesc, multipleValues: false);
            Provider = Option("--provider|-p", Resources.UpdateCommandProviderOptionDesc, CommandOptionType.SingleValue);
            PreRelease = Option("-pre", Resources.UpdateCommandPreReleaseOptionDesc, CommandOptionType.NoValue);

            return this;
        }

        protected override async Task<int> ExecuteInternalAsync()
        {
            Manifest manifest = await GetManifestAsync(createIfNotExists: false);

            IEnumerable<ILibraryInstallationState> candidates = manifest.Libraries;
            if (Provider.HasValue() && string.IsNullOrWhiteSpace(LibraryId.Value))
            {
                candidates = manifest.Libraries.Where(
                    l => l.LibraryId == LibraryId.Value
                        && (Provider.Value() == l.ProviderId
                            || (l.ProviderId == null 
                                && Provider.Value() == manifest.DefaultProvider)));

                if (candidates.Count() > 1)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            Resources.MoreThanOneLibraryFoundToUpdateForProvider,
                            LibraryId.Value,
                            Provider.Value()));
                }

                if (candidates.Count() == 0)
                {
                    throw new InvalidOperationException(
                        string.Format(Resources.NoLibraryFoundToUpdate,
                            LibraryId.Value,
                            Provider.Value()));
                }
            }
            else if (string.IsNullOrWhiteSpace(LibraryId.Value))
            {
                candidates = manifest.Libraries.Where(l => l.LibraryId == LibraryId.Value);
            }
            else if (Provider.HasValue())
            {
                candidates = manifest.Libraries.Where(l => l.ProviderId == Provider.Value()
                        || (l.ProviderId == null && Provider.Value() == manifest.DefaultProvider));
            }

            await UpdateLibrariesAsync(manifest, candidates, PreRelease.HasValue(), CancellationToken.None);

            return 0;
        }

        private async Task UpdateLibrariesAsync(Manifest manifest,
            IEnumerable<ILibraryInstallationState> candidates,
            bool includePreRelease,
            CancellationToken cancellationToken)
        {
            if (!candidates.Any())
            {
                Logger.Log(Resources.NoLibrariesToUpdate, LogLevel.Operation);
                return;
            }

            Dictionary<string, ILibraryCatalog> catalogs = new Dictionary<string, ILibraryCatalog>();

            Dictionary<ILibraryInstallationState, string> latestVersions = new Dictionary<ILibraryInstallationState, string>();

            foreach (ILibraryInstallationState candidate in candidates)
            {
                IProvider provider = ManifestDependencies.Providers.FirstOrDefault(p => p.Id == candidate.ProviderId);
                if (!catalogs.ContainsKey(provider.Id))
                {
                    catalogs[provider.Id] = provider.GetCatalog();
                }

                latestVersions[candidate] = await catalogs[provider.Id]
                    .GetLatestVersion(candidate.LibraryId, includePreRelease, cancellationToken);
            }



        }
    }
}
