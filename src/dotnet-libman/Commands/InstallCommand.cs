// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
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

            ILibraryInstallationState library = new LibraryInstallationState()
            {
                LibraryId = LibraryId.Value,
                ProviderId = Provider.Value(),
                DestinationPath = Destination.Value(),
                Files = files
            };

            manifest.AddLibrary(library);

            await manifest.SaveAsync(Settings.ManifestFileName, CancellationToken.None);

            // Reload the manifest.
            manifest = await GetManifestAsync(createIfNotExists: false);

            await ManifestRestorer.RestoreManifestAsync(manifest, Logger, CancellationToken.None);

            return 0;
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

            if (errors.Any())
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
            }
        }

        public override string Examples => Resources.InstallCommandExamples;
        public override string Remarks => Resources.InstallCommandRemarks;

    }
}
