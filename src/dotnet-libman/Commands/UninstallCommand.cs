// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.Web.LibraryManager.Tools.Commands
{
    internal class UninstallCommand : BaseCommand
    {
        public UninstallCommand(IHostEnvironment hostEnvironment, bool throwOnUnexpectedArg = true)
           : base(throwOnUnexpectedArg, "uninstall", Resources.UnInstallCommandDesc, hostEnvironment)
        {
        }

        public CommandArgument LibraryId { get; private set; }
        public CommandOption Provider { get; private set; }

        public override BaseCommand Configure(CommandLineApplication parent)
        {
            base.Configure(parent);

            LibraryId = Argument("libraryId", Resources.UninstallCommandLibraryIdArgumentDesc, false);
            Provider = Option("--provider|-p", Resources.UninstallCommandProviderOptionDesc, CommandOptionType.SingleValue);

            return this;
        }

        protected async override Task<int> ExecuteInternalAsync()
        {
            Manifest manifest = await GetManifestAsync(createIfNotExists: false);

            ValidateParameters(manifest);

            Action<string> deleteFileAction = (s) => HostInteractions.DeleteFiles(s);

            if (Provider.HasValue())
            {
                manifest.Uninstall(LibraryId.Value, Provider.Value(), deleteFileAction);
            }
            else
            {
                manifest.Uninstall(LibraryId.Value, deleteFileAction);
            }

            await manifest.SaveAsync(Settings.ManifestFileName, CancellationToken.None);

            return 0;
        }

        private void ValidateParameters(Manifest manifest)
        {
            List<string> errors = new List<string>();

            if (string.IsNullOrWhiteSpace(LibraryId.Value))
            {
                errors.Add(Resources.LibraryIdRequiredForUnInstall);
            }
            else if (!Provider.HasValue())
            {
                if (manifest.Libraries.Count(l => l.LibraryId == LibraryId.Value) > 1)
                {
                    errors.Add(string.Format(Resources.MoreThanOneLibraryFoundToUninstall, LibraryId.Value));
                    errors.Add(string.Format(Resources.UseProviderToDisambiguateMessage));
                }
            }

            if (errors.Any())
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
            }
        }

        public override string Remarks => Resources.UnInstallCommandRemarks;
        public override string Examples => Resources.UnInstallCommandExamples;
    }
}
