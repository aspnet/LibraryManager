﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.LibraryNaming;

namespace Microsoft.Web.LibraryManager.Tools.Commands
{
    /// <summary>
    /// Defines the libman install command to allow users to install libraries.
    /// </summary>
    /// <remarks>Creates a new libman.json if one doesn't exist in current directory.</remarks>
    internal class InstallCommand : BaseCommand
    {
        public InstallCommand(IHostEnvironment hostEnvironment, bool throwOnUnexpectedArg = true)
            : base(throwOnUnexpectedArg, "install", Resources.Text.InstallCommandDesc, hostEnvironment)
        {
        }

        /// <summary>
        /// Argument to specify the library to install.
        /// </summary>
        /// <remarks>LibraryId is required argument.</remarks>
        public CommandArgument LibraryId { get; private set; }

        /// <summary>
        /// Option to specify the provider to use for installing the library.
        /// </summary>
        public CommandOption Provider { get; private set; }

        /// <summary>
        /// Option to specify the destination for the library.
        /// </summary>
        public CommandOption Destination { get; set; }

        /// <summary>
        /// Option to specify files for the library.
        /// </summary>
        /// <remarks>Allows specifying multiple values</remarks>
        public CommandOption Files { get; set; }


        private Manifest _manifest;
        private IProvider _provider;
        private ILibraryCatalog _catalog;

        private string InstallDestination { get; set; }
        private string ProviderId { get; set; }

        private IProvider ProviderToUse
        {
            get
            {
                if (_provider == null)
                {
                    _provider = ManifestDependencies.GetProvider(ProviderId);
                }

                return _provider;
            }
        }

        private ILibraryCatalog ProviderCatalog
        {
            get
            {
                if (_catalog == null)
                {
                    _catalog = ProviderToUse.GetCatalog();
                }

                return _catalog;
            }
        }

        public override BaseCommand Configure(CommandLineApplication parent)
        {
            base.Configure(parent);

            LibraryId = Argument("libraryId", Resources.Text.InstallCommandLibraryIdArgumentDesc, multipleValues: false);
            Provider = Option("--provider|-p", Resources.Text.ProviderOptionDesc, CommandOptionType.SingleValue);
            Destination = Option("--destination|-d", Resources.Text.DestinationOptionDesc, CommandOptionType.SingleValue);
            Files = Option("--files", Resources.Text.FilesOptionDesc, CommandOptionType.MultipleValue);

            return this;
        }

        protected override async Task<int> ExecuteInternalAsync()
        {
            if (!File.Exists(Settings.ManifestFileName))
            {
                await CreateManifestAsync(Provider.Value(), null, Settings, Provider.LongName, CancellationToken.None);
            }

            _manifest = await GetManifestAsync();

            ValidateParameters(_manifest);

            List<string> files = Files.HasValue() ? Files.Values : null;

            (string libraryId, ILibrary library) = await ValidateLibraryExistsInCatalogAsync(CancellationToken.None);

            string providerIdToUse = Provider.Value();
            if (string.IsNullOrWhiteSpace(_manifest.DefaultProvider) && string.IsNullOrWhiteSpace(providerIdToUse))
            {
                // If there was previously no default provider and the user did not specify on commandline,
                // we get this value by prompting to the user and set it as the default for the manifest.
                _manifest.DefaultProvider = ProviderId;
            }

            InstallDestination = Destination.HasValue() ? Destination.Value() : _manifest.DefaultDestination;
            if (string.IsNullOrWhiteSpace(InstallDestination))
            {
                string destinationHint = string.Join('/', Settings.DefaultDestinationRoot, ProviderToUse.GetSuggestedDestination(library));
                InstallDestination = GetUserInputWithDefault(
                    fieldName: nameof(Destination),
                    defaultFieldValue: destinationHint,
                    optionLongName: Destination.LongName);
            }

            string destinationToUse = Destination.Value();
            if (string.IsNullOrWhiteSpace(_manifest.DefaultDestination) && string.IsNullOrWhiteSpace(destinationToUse))
            {
                destinationToUse = InstallDestination;
            }

            if (destinationToUse is not null)
            {
                // in case the user changed the suggested default, normalize separator to /
                destinationToUse = destinationToUse.Replace('\\', '/');
            }

            ILibraryOperationResult result = await _manifest.InstallLibraryAsync(
                library.Name,
                library.Version,
                providerIdToUse,
                files,
                destinationToUse,
                CancellationToken.None);

            if (result.Success)
            {
                await _manifest.SaveAsync(Settings.ManifestFileName, CancellationToken.None);
                Logger.Log(string.Format(Resources.Text.InstalledLibrary, libraryId, InstallDestination), LogLevel.Operation);
            }
            else
            {
                bool isFileConflicts = false;
                Logger.Log(string.Format(Resources.Text.InstallLibraryFailed, libraryId), LogLevel.Error);
                foreach (IError error in result.Errors)
                {
                    Logger.Log(string.Format("[{0}]: {1}", error.Code, error.Message), LogLevel.Error);
                    if (error.Code == PredefinedErrors.ConflictingFilesInManifest("", new List<string>()).Code)
                    {
                        isFileConflicts = true;
                    }
                }

                if (isFileConflicts)
                {
                    Logger.Log(Resources.Text.SpecifyDifferentDestination, LogLevel.Error);
                }
            }

            return 0;
        }

        private async Task<(string libraryId, ILibrary library)> ValidateLibraryExistsInCatalogAsync(CancellationToken cancellationToken)
        {
            (string name, string version) = LibraryIdToNameAndVersionConverter.Instance.GetLibraryNameAndVersion(LibraryId.Value, ProviderToUse.Id);
            try
            {
                ILibrary libraryToInstall = await ProviderCatalog.GetLibraryAsync(name, version, cancellationToken);

                if (libraryToInstall != null)
                {
                    return (LibraryId.Value, libraryToInstall);
                }
            }
            catch
            {
                // The library id wasn't in the exact format.
            }

            IReadOnlyList<ILibraryGroup> libraryGroup = await ProviderCatalog.SearchAsync(LibraryId.Value, 5, cancellationToken);

            IError invalidLibraryError = PredefinedErrors.UnableToResolveSource(LibraryId.Value, ProviderId);
            if (libraryGroup.Count == 0)
            {
                throw new InvalidOperationException($"[{invalidLibraryError.Code}]: {invalidLibraryError.Message}");
            }

            var sb = new StringBuilder();

            foreach (ILibraryGroup libGroup in libraryGroup)
            {
                if (libGroup.DisplayName.Equals(LibraryId.Value, StringComparison.OrdinalIgnoreCase))
                {
                    // Found a group with an exact match.
                    string latestVersion = await ProviderCatalog.GetLatestVersion(libGroup.DisplayName, false, cancellationToken);
                    string libraryId = LibraryIdToNameAndVersionConverter.Instance.GetLibraryId(libGroup.DisplayName, latestVersion, ProviderId);
                    ILibrary libraryToInstall = await ProviderCatalog.GetLibraryAsync(libGroup.DisplayName, latestVersion, cancellationToken);

                    return (libraryId, libraryToInstall);
                }

                sb.AppendLine("  " + libGroup.DisplayName);
            }

            sb.Insert(0, $"[{invalidLibraryError.Code}]: {invalidLibraryError.Message} {Environment.NewLine} {Resources.Text.SuggestedIdsMessage}{Environment.NewLine}");
            throw new InvalidOperationException(sb.ToString());
        }

        private void ValidateParameters(Manifest manifest)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(LibraryId.Value))
            {
                errors.Add(Resources.Text.LibraryIdRequiredForInstall);
            }

            ProviderId = Provider.HasValue() ? Provider.Value() : manifest.DefaultProvider;

            if (string.IsNullOrWhiteSpace(ProviderId))
            {
                ProviderId = HostEnvironment.InputReader.GetUserInputWithDefault(nameof(ProviderId), Settings.DefaultProvider);
            }

            if (!ManifestDependencies.Providers.Any(p => p.Id == ProviderId))
            {
                errors.Add(string.Format(Resources.Text.ProviderNotInstalled, ProviderId));
            }

            if (errors.Any())
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
            }
        }

        public override string Examples => Resources.Text.InstallCommandExamples;
        public override string Remarks => Resources.Text.InstallCommandRemarks;

    }
}
