// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.Web.Editor.SuggestedActions;
using System;
using System.Threading;

namespace LibraryInstaller.Vsix
{
    internal class UninstallSuggestedAction : SuggestedActionBase
    {
        private static readonly Guid _guid = new Guid("2975f71b-809d-4ed6-a170-6bbc04058424");
        private JSONObject _libraryObject;
        private string _libraryId;
        private string _configFileName;

        public UninstallSuggestedAction(ITextBuffer buffer, ITextView view, JSONObject libraryObject, string libraryId, string configFileName)
            : base(buffer, view, $"Uninstall {libraryId}", _guid)
        {
            _libraryObject = libraryObject;
            _libraryId = libraryId;
            _configFileName = configFileName;
            IconMoniker = KnownMonikers.Uninstall;
        }


        public override async void Invoke(CancellationToken cancellationToken)
        {
            try
            {
                await LibraryHelpers.UninstallAsync(_configFileName, _libraryId, cancellationToken);

                using (ITextEdit edit = TextBuffer.CreateEdit())
                {
                    var arrayElement = _libraryObject.Parent as JSONArrayElement;
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
