// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using Microsoft.Web.LibraryInstaller.Contracts;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryInstaller.Vsix.Json
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("JSON")]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    public class TextviewCreationListener : IWpfTextViewCreationListener
    {
        private Manifest _manifest;
        private Dependencies _dependencies;

        [Import]
        public ITextDocumentFactoryService DocumentService { get; set; }

        public void TextViewCreated(IWpfTextView textView)
        {
            if (!DocumentService.TryGetTextDocument(textView.TextBuffer, out var doc))
            {
                return;
            }

            string fileName = Path.GetFileName(doc.FilePath);

            if (!fileName.Equals(Constants.ConfigFileName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _dependencies = Dependencies.FromConfigFile(doc.FilePath);
            _manifest = Manifest.FromFileAsync(doc.FilePath, _dependencies, CancellationToken.None).Result;

            doc.FileActionOccurred += OnFileSaved;
            textView.Closed += OnViewClosed;
        }

        private async Task RemoveFilesAsync(Manifest newManifest)
        {
            IEnumerable<string> prevFiles = await GetAllManifestFilesAsync(_manifest).ConfigureAwait(false);
            IEnumerable<string> newFiles = await GetAllManifestFilesAsync(newManifest).ConfigureAwait(false);
            IEnumerable<string> filesToRemove = prevFiles.Where(f => !newFiles.Contains(f));

            if (filesToRemove.Any())
            {
                var hostInteraction = _dependencies.GetHostInteractions() as HostInteraction;
                hostInteraction.DeleteFiles(filesToRemove.ToArray());
            }
        }

        private async Task<IEnumerable<string>> GetAllManifestFilesAsync(Manifest manifest)
        {
            var files = new List<string>();

            foreach (ILibraryInstallationState state in manifest.Libraries.Select(l => l))
            {
                IEnumerable<string> stateFiles = await GetFilesAsync(state).ConfigureAwait(false);
                IEnumerable<string> filesToAdd = stateFiles
                    .Select(f => Path.Combine(state.DestinationPath, f))
                    .Where(f => !files.Contains(f));

                files.AddRange(filesToAdd);
            }

            return files;
        }

        private async Task<IEnumerable<string>> GetFilesAsync(ILibraryInstallationState state)
        {
            if (state.Files == null)
            {
                ILibraryCatalog catalog = _dependencies.GetProvider(state.ProviderId)?.GetCatalog();

                if (catalog != null)
                {
                    ILibrary library = await catalog?.GetLibraryAsync(state.LibraryId, CancellationToken.None);
                    return library?.Files.Keys.ToList();
                }
            }

            return state.Files.Distinct();
        }

        private void OnFileSaved(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType == FileActionTypes.ContentSavedToDisk)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        Manifest newManifest = await Manifest.FromFileAsync(e.FilePath, _dependencies, CancellationToken.None).ConfigureAwait(false);
                        await RemoveFilesAsync(newManifest).ConfigureAwait(false);

                        _manifest = newManifest;

                        await LibraryHelpers.RestoreAsync(e.FilePath, CancellationToken.None).ConfigureAwait(false);
                        Telemetry.TrackOperation("restoresave");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogEvent(ex.ToString(), Contracts.LogLevel.Error);
                        Telemetry.TrackException("configsaved", ex);
                    }
                });
            }
        }

        private void OnViewClosed(object sender, EventArgs e)
        {
            var view = (IWpfTextView)sender;

            if (DocumentService.TryGetTextDocument(view.TextBuffer, out var doc))
            {
                doc.FileActionOccurred -= OnFileSaved;
            }
        }
    }
}
