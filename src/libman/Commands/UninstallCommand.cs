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
    /// <summary>
    /// Defines the libman uninstall command
    /// </summary>
    internal class UninstallCommand : BaseCommand
    {
        public UninstallCommand(IHostEnvironment hostEnvironment, bool throwOnUnexpectedArg = true)
           : base(throwOnUnexpectedArg, "uninstall", Resources.UnInstallCommandDesc, hostEnvironment)
        {
        }

        /// <summary>
        /// Argument to specify the library to uninstall.
        /// </summary>
        /// <remarks>Required argument.</remarks>
        public CommandArgument LibraryId { get; private set; }

        /// <summary>
        /// Option to specify the provider to use to filter libraryies.
        /// </summary>
        public CommandOption Provider { get; private set; }

        public override BaseCommand Configure(CommandLineApplication parent)
        {
            base.Configure(parent);

            LibraryId = Argument("libraryId", Resources.UninstallCommandLibraryIdArgumentDesc, false);
            Provider = Option("--provider|-p", Resources.UninstallCommandProviderOptionDesc, CommandOptionType.SingleValue);

            // Reserve this.
            Provider.ShowInHelpText = false;

            return this;
        }

        protected async override Task<int> ExecuteInternalAsync()
        {
            Manifest manifest = await GetManifestAsync(createIfNotExists: false);

            IEnumerable<ILibraryInstallationState> installedLibraries = await ValidateParametersAndGetLibrariesToUninstallAsync(manifest, CancellationToken.None);

            if (installedLibraries == null || !installedLibraries.Any())
            {
                Logger.Log(string.Format(Resources.NoLibraryToUninstall, LibraryId.Value), LogLevel.Operation);
                return 0;
            }

            ILibraryInstallationState libraryToUninstall = null;

            if (installedLibraries.Count() > 1)
            {
                Logger.Log(string.Format(Resources.MoreThanOneLibraryFoundToUninstall, LibraryId.Value), LogLevel.Operation);

                libraryToUninstall = LibraryResolver.ResolveLibraryByUserChoice(installedLibraries, HostEnvironment);
            }
            else
            {
                libraryToUninstall = installedLibraries.First();
            }

            Task<bool> deleteFileAction(IEnumerable<string> s) => HostInteractions.DeleteFilesAsync(s, CancellationToken.None);

            ILibraryOperationResult result = await manifest.UninstallAsync(libraryToUninstall.LibraryId, deleteFileAction, CancellationToken.None);

            if (result.Success)
            {
                await manifest.SaveAsync(Settings.ManifestFileName, CancellationToken.None);
                Logger.Log(string.Format(Resources.UninstalledLibrary, libraryToUninstall.LibraryId), LogLevel.Operation);
            }
            else
            {
                Logger.Log(string.Format(Resources.UninstallFailed, libraryToUninstall.LibraryId), LogLevel.Error);
                foreach (IError error in result.Errors)
                {
                    Logger.Log($"[{error.Code}]: {error.Message}", LogLevel.Error);
                }
            }

            return 0;
        }

        private async Task<IEnumerable<ILibraryInstallationState>> ValidateParametersAndGetLibrariesToUninstallAsync(
            Manifest manifest,
            CancellationToken cancellationToken)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(LibraryId.Value))
            {
                errors.Add(Resources.LibraryIdRequiredForUnInstall);
            }

            IProvider provider = null;
            if (Provider.HasValue())
            {
                provider = ManifestDependencies.GetProvider(Provider.Value());
                if (provider == null)
                {
                    errors.Add(string.Format(Resources.ProviderNotInstalled, Provider.Value()));
                }
            }

            if (errors.Any())
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
            }

            return await LibraryResolver.ResolveAsync(LibraryId.Value,
                manifest,
                ManifestDependencies,
                provider,
                cancellationToken);
        }

        public override string Remarks => Resources.UnInstallCommandRemarks;
        public override string Examples => Resources.UnInstallCommandExamples;
    }
}
