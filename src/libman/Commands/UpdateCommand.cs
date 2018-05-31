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
    /// Defines the libman update command.
    /// </summary>
    internal class UpdateCommand : BaseCommand
    {
        public UpdateCommand(IHostEnvironment environment, bool throwOnUnexpectedArg = true)
            : base(throwOnUnexpectedArg, "update", Resources.UpdateCommandDesc, environment)
        {
        }

        public override string Remarks => Resources.UpdateCommandRemarks;

        public override string Examples => Resources.UpdateCommandExamples;

        /// <summary>
        /// Arugment to specify the library to update.
        /// </summary>
        /// <remarks>Required argument.</remarks>
        public CommandArgument LibraryId { get; private set; }

        /// <summary>
        /// Option to specify provider to use to filter libraries.
        /// </summary>
        public CommandOption Provider { get; private set; }

        /// <summary>
        /// Option to specify whether to allow updating to latest pre-release version where applicable.
        /// </summary>
        public CommandOption PreRelease { get; private set; }

        /// <summary>
        /// Option to specify the version to which the library should be updated.
        /// </summary>
        /// <remarks>Needs the full library id.</remarks>
        public CommandOption ToVersion { get; private set; }

        public override BaseCommand Configure(CommandLineApplication parent = null)
        {
            base.Configure(parent);

            LibraryId = Argument("libraryId", Resources.UpdateCommandLibraryArgumentDesc, multipleValues: false);
            Provider = Option("--provider|-p", Resources.UpdateCommandProviderOptionDesc, CommandOptionType.SingleValue);
            PreRelease = Option("-pre", Resources.UpdateCommandPreReleaseOptionDesc, CommandOptionType.NoValue);
            ToVersion = Option("--to", Resources.UpdateCommandToVersionOptionDesc, CommandOptionType.SingleValue);

            // Reserve this.
            Provider.ShowInHelpText = false;

            return this;
        }

        protected override async Task<int> ExecuteInternalAsync()
        {
            Manifest manifest = await GetManifestAsync(createIfNotExists: false);
            IEnumerable<ILibraryInstallationState> installedLibraries = await ValidateParametersAndGetLibrariesToUninstallAsync(manifest, CancellationToken.None);

            if (installedLibraries == null || !installedLibraries.Any())
            {
                Logger.Log(string.Format(Resources.NoLibraryFoundToUpdate, LibraryId.Value), LogLevel.Operation);
                return 0;
            }

            ILibraryInstallationState libraryToUpdate = null;

            if (installedLibraries.Count() > 1)
            {
                Logger.Log(string.Format(Resources.MoreThanOneLibraryFoundToUpdate, LibraryId.Value), LogLevel.Operation);

                libraryToUpdate = LibraryResolver.ResolveLibraryByUserChoice(installedLibraries, HostEnvironment);
            }
            else
            {
                libraryToUpdate = installedLibraries.First();
            }

            string newLibraryId = ToVersion.HasValue() ? ToVersion.Value() : null;

            if (newLibraryId == null)
            {
                newLibraryId = await GetLatestVersionAsync(libraryToUpdate, CancellationToken.None);
            }

            if (newLibraryId == null || newLibraryId == libraryToUpdate.LibraryId)
            {
                Logger.Log(string.Format(Resources.LatestVersionAlreadyInstalled, libraryToUpdate.LibraryId), LogLevel.Operation);
                return 0;
            }

            Manifest backup = manifest.Clone();
            string oldLibraryId = libraryToUpdate.LibraryId;
            manifest.ReplaceLibraryId(libraryToUpdate, newLibraryId);

            // Delete files from old version of the library.
            await backup.RemoveUnwantedFilesAsync(manifest, CancellationToken.None);

            IEnumerable<ILibraryInstallationResult> results = await manifest.RestoreAsync(CancellationToken.None);

            ILibraryInstallationResult result = null;

            foreach (ILibraryInstallationResult r in results)
            {
                if (!r.Success && r.Errors.Any(e => e.Message.Contains(libraryToUpdate.LibraryId)))
                {
                    result = r;
                    break;
                }
                else if (r.Success
                    && r.InstallationState.LibraryId == libraryToUpdate.LibraryId
                    && r.InstallationState.ProviderId == libraryToUpdate.ProviderId
                    && r.InstallationState.DestinationPath == libraryToUpdate.DestinationPath)
                {
                    result = r;
                    break;
                }
            }

            if (result.Success)
            {
                await manifest.SaveAsync(HostEnvironment.EnvironmentSettings.ManifestFileName, CancellationToken.None);
                Logger.Log(string.Format(Resources.LibraryUpdated, oldLibraryId, newLibraryId), LogLevel.Operation);
            }
            else if (result.Errors != null)
            {
                if (ToVersion.HasValue())
                {
                    Logger.Log(string.Format(Resources.UpdateLibraryFailed, oldLibraryId, ToVersion.Value()), LogLevel.Error);
                }
                else
                {
                    Logger.Log(string.Format(Resources.UpdateLibraryToLatestFailed, oldLibraryId), LogLevel.Error);
                }
                foreach (IError error in result.Errors)
                {
                    Logger.Log(string.Format("[{0}]: {1}", error.Code, error.Message), LogLevel.Error);
                }
            }

            return 0;
        }

        private async Task<string> GetLatestVersionAsync(ILibraryInstallationState libraryToUpdate, CancellationToken cancellationToken)
        {
            ILibraryCatalog catalog = ManifestDependencies.GetProvider(libraryToUpdate.ProviderId)?.GetCatalog();
            if (catalog == null)
            {
                throw new InvalidOperationException(PredefinedErrors.LibraryIdIsUndefined().Message);
            }

            try
            {
                return await catalog.GetLatestVersion(libraryToUpdate.LibraryId, PreRelease.HasValue(), cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format(Resources.UnableToFindLatestVersionForLibrary, libraryToUpdate.LibraryId), ex);
            }
        }

        private async Task<IEnumerable<ILibraryInstallationState>> ValidateParametersAndGetLibrariesToUninstallAsync(
            Manifest manifest,
            CancellationToken cancellationToken)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(LibraryId.Value))
            {
                errors.Add(Resources.LibraryIdRequiredForUpdate);
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

            if (ToVersion.HasValue() && string.IsNullOrWhiteSpace(ToVersion.Value()))
            {
                errors.Add(string.Format(Resources.InvalidToVersion, ToVersion.Value()));
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
    }
}
