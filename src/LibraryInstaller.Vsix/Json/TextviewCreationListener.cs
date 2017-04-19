// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.LibraryInstaller.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryInstaller.Vsix.Json
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("JSON")]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    public class TextviewCreationListener : IVsTextViewCreationListener
    {
        private Manifest _manifest;
        private Dependencies _dependencies;

        [Import]
        public ITextDocumentFactoryService DocumentService { get; set; }

        [Import]
        IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import]
        ICompletionBroker CompletionBroker { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            IWpfTextView textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);
            new CompletionController(textViewAdapter, textView, CompletionBroker);

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

            foreach (ILibraryInstallationState state in manifest.Libraries.Where(l => l.IsValid(out var errors)))
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
                        Logger.LogEvent(ex.ToString(), LogLevel.Error);
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
