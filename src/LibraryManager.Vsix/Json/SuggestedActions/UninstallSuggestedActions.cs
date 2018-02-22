// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Text;
using Microsoft.Web.Editor.SuggestedActions;
using System;
using System.Threading;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal class UninstallSuggestedAction : SuggestedActionBase
    {
        private static readonly Guid _guid = new Guid("2975f71b-809d-4ed6-a170-6bbc04058424");
        private readonly SuggestedActionProvider _provider;
        private const int _maxlength = 40;

        public UninstallSuggestedAction(SuggestedActionProvider provider)
            : base(provider.TextBuffer, provider.TextView, GetDisplayText(provider), _guid)
        {
            _provider = provider;
            IconMoniker = KnownMonikers.Cancel;
        }

        private static string GetDisplayText(SuggestedActionProvider provider)
        {
            string cleanId = provider.InstallationState.LibraryId;

            if (cleanId.Length > _maxlength + 10)
            {
                cleanId = $"...{cleanId.Substring(cleanId.Length - _maxlength)}";
            }

            return string.Format(Resources.Text.UninstallLibrary, cleanId);
        }

        public override async void Invoke(CancellationToken cancellationToken)
        {
            try
            {
                await LibraryHelpers.UninstallAsync(_provider.ConfigFilePath, _provider.InstallationState.LibraryId, cancellationToken).ConfigureAwait(false);

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                using (ITextEdit edit = TextBuffer.CreateEdit())
                {
                    var arrayElement = _provider.LibraryObject.Parent as JSONArrayElement;
                    var prev = arrayElement.PreviousSibling as JSONArrayElement;
                    var next = arrayElement.NextSibling as JSONArrayElement;

                    int start = TextBuffer.CurrentSnapshot.GetLineFromPosition(arrayElement.Start).Start;
                    int end = TextBuffer.CurrentSnapshot.GetLineFromPosition(arrayElement.AfterEnd).EndIncludingLineBreak;

                    if (next == null && prev?.Comma != null)
                    {
                        start = prev.Comma.Start;
                        end = TextBuffer.CurrentSnapshot.GetLineFromPosition(arrayElement.AfterEnd).End;
                    }

                    edit.Delete(Span.FromBounds(start, end));
                    edit.Apply();
                }
            }
            catch (Exception ex)
            {
                Logger.LogEvent(ex.ToString(), Contracts.LogLevel.Error);
            }
        }
    }
}
