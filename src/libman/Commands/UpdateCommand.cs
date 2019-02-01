// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.LibraryNaming;

namespace Microsoft.Web.LibraryManager.Tools.Commands
{
    /// <summary>
    /// Defines the libman update command.
    /// </summary>
    internal class UpdateCommand : BaseCommand
    {
        public UpdateCommand(IHostEnvironment environment, bool throwOnUnexpectedArg = true)
            : base(throwOnUnexpectedArg, "update", Resources.Text.UpdateCommandDesc, environment)
        {
        }

        public override string Remarks => Resources.Text.UpdateCommandRemarks;

        public override string Examples => Resources.Text.UpdateCommandExamples;

        /// <summary>
        /// Arugment to specify the library to update.
        /// </summary>
        /// <remarks>Required argument.</remarks>
        public CommandArgument LibraryName { get; private set; }

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

            LibraryName = Argument("libraryName", Resources.Text.UpdateCommandLibraryArgumentDesc, multipleValues: false);
            Provider = Option("--provider|-p", Resources.Text.UpdateCommandProviderOptionDesc, CommandOptionType.SingleValue);
            PreRelease = Option("-pre", Resources.Text.UpdateCommandPreReleaseOptionDesc, CommandOptionType.NoValue);
            ToVersion = Option("--to", Resources.Text.UpdateCommandToVersionOptionDesc, CommandOptionType.SingleValue);

            // Reserve this.
            Provider.ShowInHelpText = false;

            return this;
        }

        protected override async Task<int> ExecuteInternalAsync()
        {
            Manifest manifest = await GetManifestAsync();
            IEnumerable<ILibraryOperationResult> validationResults = await manifest.GetValidationResultsAsync(CancellationToken.None);

            if (!validationResults.All(r => r.Success))
            {
                LogErrors(validationResults.SelectMany(r => r.Errors));

                return 0;
            }

            IEnumerable<ILibraryInstallationState> installedLibraries = ValidateParametersAndGetLibrariesToUpdate(manifest);

            if (installedLibraries == null || !installedLibraries.Any())
            {
                Logger.Log(string.Format(Resources.Text.NoLibraryFoundToUpdate, LibraryName.Value), LogLevel.Operation);
                return 0;
            }

            ILibraryInstallationState libraryToUpdate = null;

            if (installedLibraries.Count() > 1)
            {
                Logger.Log(string.Format(Resources.Text.MoreThanOneLibraryFoundToUpdate, LibraryName.Value), LogLevel.Operation);

                libraryToUpdate = LibraryResolver.ResolveLibraryByUserChoice(installedLibraries, HostEnvironment);
            }
            else
            {
                libraryToUpdate = installedLibraries.First();
            }

            string newVersion = ToVersion.HasValue()
                ? ToVersion.Value()
                : await GetLatestVersionAsync(libraryToUpdate, CancellationToken.None);

            if (newVersion == null || newVersion == libraryToUpdate.Version)
            {
                Logger.Log(string.Format(Resources.Text.LatestVersionAlreadyInstalled, libraryToUpdate.Name), LogLevel.Operation);
                return 0;
            }

            Manifest backup = manifest.Clone();
            string oldLibraryName = libraryToUpdate.Name;
            Manifest.UpdateLibraryVersion(libraryToUpdate, newVersion);

            // Delete files from old version of the library.
            await backup.RemoveUnwantedFilesAsync(manifest, CancellationToken.None);

            IEnumerable<ILibraryOperationResult> results = await manifest.RestoreAsync(CancellationToken.None);

            ILibraryOperationResult result = null;

            foreach (ILibraryOperationResult r in results)
            {
                if (!r.Success && r.Errors.Any(e => e.Message.Contains(libraryToUpdate.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    result = r;
                    break;
                }
                else if (r.Success
                    && r.InstallationState.Name  == libraryToUpdate.Name
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
                Logger.Log(string.Format(Resources.Text.LibraryUpdated, oldLibraryName, newVersion), LogLevel.Operation);
            }
            else if (result.Errors != null)
            {
                if (ToVersion.HasValue())
                {
                    Logger.Log(string.Format(Resources.Text.UpdateLibraryFailed, oldLibraryName, ToVersion.Value()), LogLevel.Error);
                }
                else
                {
                    Logger.Log(string.Format(Resources.Text.UpdateLibraryToLatestFailed, oldLibraryName), LogLevel.Error);
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
                return await catalog.GetLatestVersion(libraryToUpdate.Name, PreRelease.HasValue(), cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format(Resources.Text.UnableToFindLatestVersionForLibrary, libraryToUpdate.Name), ex);
            }
        }

        private IEnumerable<ILibraryInstallationState> ValidateParametersAndGetLibrariesToUpdate(Manifest manifest)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(LibraryName.Value))
            {
                errors.Add(Resources.Text.LibraryIdRequiredForUpdate);
            }

            IProvider provider = null;
            if (Provider.HasValue())
            {
                provider = ManifestDependencies.GetProvider(Provider.Value());
                if (provider == null)
                {
                    errors.Add(string.Format(Resources.Text.ProviderNotInstalled, Provider.Value()));
                }
            }

            if (ToVersion.HasValue() && string.IsNullOrWhiteSpace(ToVersion.Value()))
            {
                errors.Add(string.Format(Resources.Text.InvalidToVersion, ToVersion.Value()));
            }

            if (errors.Any())
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
            }

            return manifest.Libraries.Where(l => l.Name.Equals(LibraryName.Value, StringComparison.OrdinalIgnoreCase));
        }
    }
}
