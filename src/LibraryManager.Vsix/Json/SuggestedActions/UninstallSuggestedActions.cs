﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.LibraryNaming;
using Microsoft.Web.LibraryManager.Vsix.Contracts;
using Microsoft.Web.LibraryManager.Vsix.Shared;
using Microsoft.WebTools.Languages.Json.Parser.Nodes;
using Microsoft.WebTools.Languages.Shared.Editor.SuggestedActions;
using Microsoft.WebTools.Languages.Shared.Parser.Nodes;
using Microsoft.WebTools.Languages.Shared.Utility;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.Web.LibraryManager.Vsix.Json.SuggestedActions
{
    internal class UninstallSuggestedAction : SuggestedActionBase
    {
        private static readonly Guid Guid = new Guid("2975f71b-809d-4ed6-a170-6bbc04058424");
        private const int MaxLength = 40;

        private readonly SuggestedActionProvider _provider;
        private readonly ILibraryCommandService _libraryCommandService;

        public UninstallSuggestedAction(SuggestedActionProvider provider, ILibraryCommandService libraryCommandService)
            : base(provider.TextBuffer, provider.TextView, GetDisplayText(provider), Guid)
        {
            _libraryCommandService = libraryCommandService;
            _provider = provider;
            IconMoniker = KnownMonikers.Cancel;
        }

        private static string GetDisplayText(SuggestedActionProvider provider)
        {
            ILibraryInstallationState state = provider.InstallationState;
            string cleanId = LibraryIdToNameAndVersionConverter.Instance.GetLibraryId(state.Name, state.Version, state.ProviderId);

            if (cleanId.Length > MaxLength + 10)
            {
                cleanId = $"...{cleanId.Substring(cleanId.Length - MaxLength)}";
            }

            return string.Format(Resources.Text.UninstallLibrary, cleanId);
        }

        public override void Invoke(CancellationToken cancellationToken)
        {
            _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await InvokeAsync(cancellationToken);
            });
        }

        private async Task InvokeAsync(CancellationToken cancellationToken)
        {
            try
            {
                Telemetry.TrackUserTask("Invoke-UninstallFromSuggestedAction");
                ILibraryInstallationState state = _provider.InstallationState;
                await _libraryCommandService.UninstallAsync(_provider.ConfigFilePath, state.Name, state.Version, state.ProviderId, cancellationToken)
                    .ConfigureAwait(false);

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                using (ITextEdit edit = TextBuffer.CreateEdit())
                {
                    var arrayElement = _provider.LibraryObject.Parent as ArrayElementNode;
                    var prev = GetPreviousSibling(arrayElement) as ArrayElementNode;
                    var next = GetNextSibling(arrayElement) as ArrayElementNode;

                    int start = TextBuffer.CurrentSnapshot.GetLineFromPosition(arrayElement.Start).Start;
                    int end = TextBuffer.CurrentSnapshot.GetLineFromPosition(arrayElement.End).EndIncludingLineBreak;

                    if (next == null && prev?.Comma != null)
                    {
                        start = prev.Comma.Start;
                        end = TextBuffer.CurrentSnapshot.GetLineFromPosition(arrayElement.End).End;
                    }

                    edit.Delete(Span.FromBounds(start, end));
                    edit.Apply();
                }
            }
            catch (Exception ex)
            {
                Logger.LogEvent(ex.ToString(), LogLevel.Error);
                Telemetry.TrackException("UninstallFromSuggestedActionFailed", ex);
            }
        }

        private Node GetPreviousSibling(ArrayElementNode arrayElementNode)
        {
            ComplexNode parent = arrayElementNode.Parent as ComplexNode;
            SortedNodeList<Node> children = JsonHelpers.GetChildren(parent);

            return parent != null ? GetPreviousChild(arrayElementNode, children) : null;
        }

        private Node GetNextSibling(ArrayElementNode arrayElementNode)
        {
            ComplexNode parent = arrayElementNode.Parent as ComplexNode;
            SortedNodeList<Node> children = JsonHelpers.GetChildren(parent);

            return parent != null ? GetNextChild(arrayElementNode, children) : null;
        }

        private Node GetPreviousChild(Node child, SortedNodeList<Node> children)
        {
            int index = (child != null) ? children.IndexOf(child) : -1;

            if (index > 0)
            {
                return children[index - 1];
            }

            return null;
        }

        private Node GetNextChild(Node child, SortedNodeList<Node> children)
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
