// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Text;
using Microsoft.Web.Editor.SuggestedActions;
using System;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.JSON.Core.Parser;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal class UninstallSuggestedAction : SuggestedActionBase
    {
        private static readonly Guid _guid = new Guid("2975f71b-809d-4ed6-a170-6bbc04058424");
        private readonly SuggestedActionProvider _provider;
        private readonly ILibraryCommandService _libraryCommandService;
        private const int _maxlength = 40;

        public UninstallSuggestedAction(SuggestedActionProvider provider, ILibraryCommandService libraryCommandService)
            : base(provider.TextBuffer, provider.TextView, GetDisplayText(provider), _guid)
        {
            _libraryCommandService = libraryCommandService;
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
                await _libraryCommandService.UninstallAsync(_provider.ConfigFilePath, _provider.InstallationState.LibraryId, cancellationToken).ConfigureAwait(false);

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                using (ITextEdit edit = TextBuffer.CreateEdit())
                {
                    var arrayElement = _provider.LibraryObject.Parent as JSONArrayElement;
                    var prev = GetPreviousSibling(arrayElement) as JSONArrayElement;
                    var next = GetNextSibling(arrayElement) as JSONArrayElement;

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
                Logger.LogEvent(ex.ToString(), LibraryManager.Contracts.LogLevel.Error);
            }
        }

        private JSONParseItem GetPreviousSibling(JSONArrayElement arrayElement)
        {
            JSONComplexItem parent = arrayElement.Parent;
            return parent != null ? GetPreviousChild(arrayElement, parent.Children) : null;
        }

        private JSONParseItem GetNextSibling(JSONArrayElement arrayElement)
        {
            JSONComplexItem parent = arrayElement.Parent;
            return parent != null ? GetNextChild(arrayElement, parent.Children) : null;
        }

        private JSONParseItem GetPreviousChild(JSONParseItem child, JSONParseItemList children)
        {
            int index = (child != null) ? children.IndexOf(child) : -1;

            if (index > 0)
            {
                return children[index - 1];
            }

            return null;
        }

        private JSONParseItem GetNextChild(JSONParseItem child, JSONParseItemList children)
        {
            int index = (child != null) ? children.IndexOf(child) : -1;

            if (index != -1 && index + 1 < children.Count)
            {
                return children[index + 1];
            }

            return null;
        }
    }
}
