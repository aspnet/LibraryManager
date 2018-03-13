// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.LibraryManager.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Vsix.Json
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
            IEnumerable<FileIdentifier> prevFiles = await GetAllManifestFilesWithVersionsAsync(_manifest).ConfigureAwait(false);
            IEnumerable<FileIdentifier> newFiles = await GetAllManifestFilesWithVersionsAsync(newManifest).ConfigureAwait(false);
            IEnumerable<string> filesToRemove = prevFiles.Where(f => !newFiles.Contains(f)).Select(f => f.Path);

            if (filesToRemove.Any())
            {
                var hostInteraction = _dependencies.GetHostInteractions() as HostInteraction;
                hostInteraction.DeleteFiles(filesToRemove.ToArray());
            }
        }

        private async Task<IEnumerable<FileIdentifier>> GetAllManifestFilesWithVersionsAsync(Manifest manifest)
        {
            var files = new List<FileIdentifier>();

            foreach (ILibraryInstallationState state in manifest.Libraries.Where(l => l.IsValid(out var errors)))
            {
                IEnumerable<FileIdentifier> stateFiles = await GetFilesWithVersionsAsync(state).ConfigureAwait(false);

                foreach (FileIdentifier fileIdentifier in stateFiles)
                {
                    if (!files.Contains(fileIdentifier))
                    {
                        files.Add(fileIdentifier);
                    }
                }
            }

            return files;
        }

        private async Task<IEnumerable<FileIdentifier>> GetFilesWithVersionsAsync(ILibraryInstallationState state)
        {
            ILibraryCatalog catalog = _dependencies.GetProvider(state.ProviderId)?.GetCatalog();
            IEnumerable<FileIdentifier> filesWithVersions = new List<FileIdentifier>();

            if (catalog != null)
            {
                ILibrary library = await catalog?.GetLibraryAsync(state.LibraryId, CancellationToken.None);
                filesWithVersions = library?.Files.Select(f => new FileIdentifier(Path.Combine(state.DestinationPath, f.Key), library.Version));
            }

            return filesWithVersions;
        }

        private void OnFileSaved(object sender, TextDocumentFileActionEventArgs e)
        {
            var textDocument = sender as ITextDocument;

            if (e.FileActionType == FileActionTypes.ContentSavedToDisk && textDocument != null)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        Manifest newManifest = Manifest.FromJson(textDocument.TextBuffer.CurrentSnapshot.GetText(), _dependencies);
                        await RemoveFilesAsync(newManifest).ConfigureAwait(false);

                        _manifest = newManifest;

                        await LibraryHelpers.RestoreAsync(textDocument.FilePath, _manifest, CancellationToken.None).ConfigureAwait(false);
                        Telemetry.TrackOperation("restoresave");
                    }
                    catch (Exception ex)
                    {
                        string textMessage = string.Concat(Environment.NewLine, LibraryManager.Resources.Text.RestoreHasErrors, Environment.NewLine);

                        Logger.LogEvent(textMessage, LogLevel.Task);
                        Logger.LogEvent(ex.ToString(), LogLevel.Error);
                        Telemetry.TrackException("restoresavefailed", ex);
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
