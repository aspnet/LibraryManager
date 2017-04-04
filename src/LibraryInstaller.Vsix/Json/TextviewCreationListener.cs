// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using LibraryInstaller.Contracts;
using System.Linq;
using System.Collections.Generic;

namespace LibraryInstaller.Vsix.Json
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("JSON")]
    [TextViewRole(PredefinedTextViewRoles.Debuggable)]
    public class TextviewCreationListener : IWpfTextViewCreationListener
    {
        private Manifest _manifest;
        private Dependencies _dependencies;

        [Import]
        private ITextDocumentFactoryService DocumentService { get; set; }

        public void TextViewCreated(IWpfTextView textView)
        {
            if (!DocumentService.TryGetTextDocument(textView.TextBuffer, out var doc))
                return;

            string fileName = Path.GetFileName(doc.FilePath);

            if (!fileName.Equals(Constants.ConfigFileName, StringComparison.OrdinalIgnoreCase))
                return;

            _dependencies = Dependencies.FromConfigFile(doc.FilePath);
            _manifest = Manifest.FromFileAsync(doc.FilePath, _dependencies, CancellationToken.None).Result;

            doc.FileActionOccurred += OnFileSavedAsync;
            textView.Closed += OnViewClosed;
        }

        private void RemoveFiles(Manifest newManifest)
        {
            var hostInteraction = _dependencies.GetHostInteractions() as HostInteraction;

            foreach (ILibraryInstallationState existing in _manifest.Libraries)
            {
                ILibraryInstallationState ost = newManifest.Libraries.FirstOrDefault(l => l.LibraryId == existing.LibraryId && l.ProviderId == existing.ProviderId);

                if (ost == null || ost.DestinationPath != existing.DestinationPath)
                {
                    hostInteraction.DeleteFiles(existing.Files.Select(f => Path.Combine(existing.DestinationPath, f)).ToArray());
                }
                else
                {
                    IEnumerable<string> files = existing.Files.Where(f => !ost.Files.Contains(f));
                    hostInteraction.DeleteFiles(files.Select(f => Path.Combine(existing.DestinationPath, f)).ToArray());
                }
            }
        }

        private async void OnFileSavedAsync(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType == FileActionTypes.ContentSavedToDisk)
            {
                try
                {
                    Manifest newManifest = Manifest.FromFileAsync(e.FilePath, _dependencies, CancellationToken.None).Result;
                    RemoveFiles(newManifest);
                    _manifest = newManifest;


                    await LibraryHelpers.RestoreAsync(e.FilePath, CancellationToken.None);
                    Telemetry.TrackOperation("restoresave");
                }
                catch (Exception ex)
                {
                    Logger.LogEvent(ex.ToString(), Contracts.LogLevel.Error);
                    Telemetry.TrackException("configsaved", ex);
                }
            }
        }

        private void OnViewClosed(object sender, EventArgs e)
        {
            var view = (IWpfTextView)sender;

            if (DocumentService.TryGetTextDocument(view.TextBuffer, out var doc))
            {
                doc.FileActionOccurred -= OnFileSavedAsync;
            }
        }
    }
}
