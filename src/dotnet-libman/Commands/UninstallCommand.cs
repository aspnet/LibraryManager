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

            ILibraryInstallationState installedLibrary = ValidateParametersAndGetLibraryToUninstall(manifest);

            if (installedLibrary == null)
            {
                Logger.Log(string.Format(Resources.NoLibraryToUninstall, LibraryId.Value), LogLevel.Operation);
            }

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

        private ILibraryInstallationState ValidateParametersAndGetLibraryToUninstall(Manifest manifest)
        {
            List<string> errors = new List<string>();
            IEnumerable<ILibraryInstallationState> candidates = null;
            if (string.IsNullOrWhiteSpace(LibraryId.Value))
            {
                errors.Add(Resources.LibraryIdRequiredForUnInstall);
            }
            else if (!Provider.HasValue())
            {
                candidates = manifest.Libraries.Where(l => l.LibraryId == LibraryId.Value);
                if (candidates.Count() > 1)
                {
                    errors.Add(string.Format(Resources.MoreThanOneLibraryFoundToUninstall, LibraryId.Value));
                    errors.Add(string.Format(Resources.UseProviderToDisambiguateMessage));
                }
            }
            else
            {
                 candidates = manifest.Libraries.Where(
                    l => l.LibraryId == LibraryId.Value
                    && (l.ProviderId == Provider.Value()
                        || (string.IsNullOrEmpty(l.ProviderId) && Provider.Value() == manifest.DefaultProvider)));

                if (candidates.Count() > 1)
                {
                    errors.Add(string.Format(Resources.MoreThanOneLibraryFoundToUninstallForProvider, LibraryId.Value, Provider.Value()));
                }
            }

            if (errors.Any())
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
            }

            return candidates?.FirstOrDefault();
        }

        public override string Remarks => Resources.UnInstallCommandRemarks;
        public override string Examples => Resources.UnInstallCommandExamples;
    }
}
