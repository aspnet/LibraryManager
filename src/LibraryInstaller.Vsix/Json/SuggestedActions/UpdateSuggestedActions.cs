// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using LibraryInstaller.Contracts;
using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.VisualStudio.Text;
using Microsoft.Web.Editor.SuggestedActions;
using System;
using System.Linq;
using System.Threading;
using System.Windows;

namespace LibraryInstaller.Vsix
{
    internal class UpdateSuggestedAction : SuggestedActionBase
    {
        private static readonly Guid _guid = new Guid("2975f71b-809a-4ed6-a170-6bbc04058424");
        private SuggestedActionProvider _provider;

        public UpdateSuggestedAction(SuggestedActionProvider provider)
            : base(provider.TextBuffer, provider.TextView, $"Check for updates", _guid)
        {
            _provider = provider;
        }

        public override async void Invoke(CancellationToken cancellationToken)
        {
            try
            {
                var dependencies = Dependencies.FromConfigFile(_provider.ConfigFilePath);
                IProvider provider = dependencies.GetProvider(_provider.InstallationState.ProviderId);
                ILibraryCatalog catalog = provider?.GetCatalog();

                if (catalog == null)
                {
                    return;
                }

                string latest = await catalog.GetLatestVersion(_provider.InstallationState.LibraryId, false, cancellationToken);

                if (latest == null || latest == _provider.InstallationState.LibraryId)
                {
                    MessageBox.Show("No updates found at this time", Vsix.Name, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                JSONMember member = _provider.LibraryObject.Children.OfType<JSONMember>().FirstOrDefault(m => m.UnquotedNameText == "id");

                if (member != null)
                {
                    using (ITextEdit edit = TextBuffer.CreateEdit())
                    {
                        edit.Replace(new Span(member.Value.Start, member.Value.Length), "\"" + latest + "\"");
                        edit.Apply();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogEvent(ex.ToString(), LogLevel.Error);
            }
        }
    }
}
