// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryInstaller.Contracts;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.Web.Editor.SuggestedActions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryInstaller.Vsix
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

            Telemetry.TrackUserTask("checkforupdates");

            return new[] { new SuggestedActionSet(list, "Update library") };
        }

        private async Task<List<ISuggestedAction>> GetListOfActionsAsync(ILibraryCatalog catalog, CancellationToken cancellationToken)
        {
            var list = new List<ISuggestedAction>();

            string latestStable = await catalog.GetLatestVersion(_provider.InstallationState.LibraryId, false, cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(latestStable) && latestStable != _provider.InstallationState.LibraryId)
            {
                list.Add(new UpdateSuggestedAction(_provider, latestStable, $"Stable: {latestStable}"));
            }

            string latestPre = await catalog.GetLatestVersion(_provider.InstallationState.LibraryId, true, cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(latestPre) && latestPre != _provider.InstallationState.LibraryId && latestPre != latestStable)
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
