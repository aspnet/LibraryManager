// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.LibraryNaming;
using Microsoft.WebTools.Languages.Shared.Editor.SuggestedActions;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal class UpdateSuggestedActionSet : SuggestedActionBase
    {
        private static readonly Guid _guid = new Guid("2975f71b-809a-4ed6-a170-6bbc04058424");
        private readonly SuggestedActionProvider _provider;
        private Task<List<ISuggestedAction>> _actions;

        public UpdateSuggestedActionSet(SuggestedActionProvider provider)
            : base(provider.TextBuffer, provider.TextView, Resources.Text.CheckForUpdates, _guid)
        {
            _provider = provider;
        }

        public override bool HasActionSets
        {
            get
            {
                var dependencies = Dependencies.FromConfigFile(_provider.ConfigFilePath);
                IProvider provider = dependencies.GetProvider(_provider.InstallationState.ProviderId);
                ILibraryCatalog catalog = provider?.GetCatalog();

                if (catalog == null)
                {
                    return false;
                }

                _actions = GetListOfActionsAsync(catalog, CancellationToken.None);

                if (_actions.IsCompleted && _actions.Result.Count == 0)
                    return false;

                return true;
            }
        }

        public override async Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await GetActionSetAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Telemetry.TrackException(nameof(GetActionSetsAsync), ex);
                return null;
            }
        }

        private async Task<IEnumerable<SuggestedActionSet>> GetActionSetAsync()
        {
            List<ISuggestedAction> list = await _actions;

            if (list.Count == 0)
            {
                list.Add(new UpdateSuggestedAction(_provider, null, "No updates found", true));
            }

            Telemetry.TrackUserTask("Invoke-SuggestedActionCheckForUpdates");

            return new[] { new SuggestedActionSet(list, "Update library") };
        }

        private async Task<List<ISuggestedAction>> GetListOfActionsAsync(ILibraryCatalog catalog, CancellationToken cancellationToken)
        {
            var list = new List<ISuggestedAction>();
            string latestStableVersion = await catalog.GetLatestVersion(_provider.InstallationState.Name, false, cancellationToken).ConfigureAwait(false);
            string latestStable = LibraryIdToNameAndVersionConverter.Instance.GetLibraryId(
                            _provider.InstallationState.Name,
                            latestStableVersion,
                            _provider.InstallationState.ProviderId);

            if (!string.IsNullOrEmpty(latestStableVersion) && latestStableVersion != _provider.InstallationState.Version)
            {
                list.Add(new UpdateSuggestedAction(_provider, latestStable, $"Stable: {latestStable}"));
            }

            string latestPreVersion = await catalog.GetLatestVersion(_provider.InstallationState.Name, true, cancellationToken).ConfigureAwait(false);
            string latestPre = LibraryIdToNameAndVersionConverter.Instance.GetLibraryId(_provider.InstallationState.Name,
                            latestPreVersion,
                            _provider.InstallationState.ProviderId);

            if (!string.IsNullOrEmpty(latestPreVersion) && latestPreVersion != _provider.InstallationState.Version && latestPre != latestStable)
            {
                list.Add(new UpdateSuggestedAction(_provider, latestPre, $"Pre-release: {latestPre}"));
            }

            return list;
        }

        public override void Invoke(CancellationToken cancellationToken)
        {
        }
    }
}
