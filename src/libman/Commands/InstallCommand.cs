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
    internal class InstallCommand : BaseCommand
    {
        public InstallCommand(IHostEnvironment hostEnvironment, bool throwOnUnexpectedArg = true)
            : base(throwOnUnexpectedArg, "install", Resources.InstallCommandDesc, hostEnvironment)
        {
        }

        public CommandArgument LibraryId { get; private set; }
        public CommandOption Provider { get; private set; }
        public CommandOption Destination { get; set; }
        public CommandOption Files { get; set; }

        public override BaseCommand Configure(CommandLineApplication parent)
        {
            base.Configure(parent);

            LibraryId = Argument("libraryId", Resources.InstallCommandLibraryIdArgumentDesc, multipleValues: false);
            Provider = Option("--provider|-p", Resources.ProviderOptionDesc, CommandOptionType.SingleValue);
            Destination = Option("--destination|-d", Resources.DestinationOptionDesc, CommandOptionType.SingleValue);
            Files = Option("--files", Resources.FilesOptionDesc, CommandOptionType.MultipleValue);

            return this;
        }

        protected override async Task<int> ExecuteInternalAsync()
        {
            Manifest manifest = await GetManifestAsync(createIfNotExists: true);

            ValidateParameters(manifest);

            List<string> files = Files.HasValue() ? Files.Values : null;

            string providerToUse = Provider.HasValue() ? Provider.Value() : manifest.DefaultProvider;
            await ValidateLibraryExistsInCatalogAsync(providerToUse, CancellationToken.None);

            ILibraryInstallationResult result = await manifest.InstallLibraryAsync(LibraryId.Value, Provider.Value(), files, Destination.Value(), CancellationToken.None);

            if (result.Success)
            {
                await manifest.SaveAsync(Settings.ManifestFileName, CancellationToken.None);
            }
            else if (result.Errors != null)
            {
                Logger.Log(Resources.InstallLibraryFailed, LogLevel.Error);
                foreach (IError error in result.Errors)
                {
                    Logger.Log(string.Format("[{0}]: {1}", error.Code, error.Message), LogLevel.Error);
                }
            }
            else
            {
                // Just output failed.
            }

            return 0;
        }

        private async Task<ILibrary> ValidateLibraryExistsInCatalogAsync(string providerToUse, CancellationToken cancellationToken)
        {
            ILibraryCatalog providerCatalog = GetProviderCatalog(providerToUse);
            ILibrary libraryToInstall = null;
            try
            {
                libraryToInstall = await providerCatalog.GetLibraryAsync(LibraryId.Value, cancellationToken);
            }
            catch { }

            if (libraryToInstall != null)
            {
                return libraryToInstall;
            }

            IReadOnlyList<ILibraryGroup> libraryGroup = await providerCatalog.SearchAsync(LibraryId.Value, 5, cancellationToken);

            IError invalidLibraryError = PredefinedErrors.UnableToResolveSource(LibraryId.Value, providerToUse);
            if (libraryGroup.Count == 0)
            {
                throw new InvalidOperationException($"[{invalidLibraryError.Code}]: {invalidLibraryError.Message}");
            }

            StringBuilder sb = new StringBuilder();

            foreach (ILibraryGroup libGroup in libraryGroup)
            {
                var libIds = await libGroup.GetLibraryIdsAsync(cancellationToken);
                if (libIds == null || !libIds.Any())
                {
                    continue;
                }

                sb.AppendLine("  " + libIds.First());
            }

            sb.Insert(0, $"[{invalidLibraryError.Code}]: {invalidLibraryError.Message} {Environment.NewLine} {Resources.SuggestedIdsMessage}{Environment.NewLine}");
            throw new InvalidOperationException(sb.ToString());
        }

        private ILibraryCatalog GetProviderCatalog(string providerId)
        {
            IProvider provider = ManifestDependencies.Providers.FirstOrDefault(p => p.Id == providerId);

            return provider.GetCatalog();
        }

        private void ValidateParameters(Manifest manifest)
        {
            List<string> errors = new List<string>();

            if (string.IsNullOrWhiteSpace(LibraryId.Value))
            {
                errors.Add(Resources.LibraryIdRequiredForInstall);
            }

            if (string.IsNullOrWhiteSpace(manifest.DefaultDestination) && !Destination.HasValue())
            {
                errors.Add(Resources.DestinationRequiredWhenNoDefaultIsPresent);
            }

            if (string.IsNullOrWhiteSpace(manifest.DefaultProvider) && !Provider.HasValue())
            {
                errors.Add(Resources.ProviderRequiredWhenNoDefaultIsPresent);
            }

            string providerId = Provider.HasValue() ? Provider.Value() : manifest.DefaultProvider;

            if (!ManifestDependencies.Providers.Any(p => p.Id == providerId))
            {
                errors.Add(string.Format(Resources.ProviderNotInstalled, providerId));
            }

            if (errors.Any())
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
            }
        }

        public override string Examples => Resources.InstallCommandExamples;
        public override string Remarks => Resources.InstallCommandRemarks;

    }
}
